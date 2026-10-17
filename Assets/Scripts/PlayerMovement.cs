using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField] private SpriteRenderer map;
    [SerializeField] private Camera followCamera;
    private SpriteRenderer character;
    private HeroWalkAnimation walkingAnimation;
    private Vector2 facing = Vector2.down;
    private float walkedDistance;

    private void Start()
    {
        if (character != null) walkingAnimation = new HeroWalkAnimation(character.sprite);
        ShowWalkingFrame(0);
    }

    private void Awake()
    {
        character = GetComponent<SpriteRenderer>();
        // Foreshorten the supplied front-facing artwork in its local plane before heading rotation.
        var spriteScale = transform.localScale;
        transform.localScale = new Vector3(spriteScale.x * 0.85f, spriteScale.y * 0.85f, spriteScale.z);
        if (followCamera == null)
            followCamera = Camera.main;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var game = RpgGame.Instance;
        if (keyboard == null || !Application.isFocused || game != null && game.BlockMovement)
        {
            StopWalking();
            return;
        }

        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
        MoveInDirection(new Vector2(x, y), Time.deltaTime);
    }

    public void MoveInDirection(Vector2 input, float deltaTime)
    {
        var game = RpgGame.Instance;
        var direction = Vector2.ClampMagnitude(input, 1f);
        if (direction.sqrMagnitude > 0.01f) facing = direction.normalized;
        if (game != null && direction.sqrMagnitude > 0.01f)
            game.Facing = direction.normalized;
        var position = transform.position + (Vector3)(direction * (moveSpeed * (game != null ? game.SpeedMultiplier : 1) * Mathf.Max(0, deltaTime)));
        if (map != null)
        {
            var bounds = map.bounds;
            // Transparent padding and the changing walk frame must not change collision size.
            var margin = new Vector3(0.2f, 0.2f, 0);
            position.x = ClampToMap(position.x, bounds.min.x, bounds.max.x, margin.x);
            position.y = ClampToMap(position.y, bounds.min.y, bounds.max.y, margin.y);
            position = RpgWorldGeometry.Move(transform.position, position, bounds);
        }
        float distance = Vector2.Distance(transform.position, position);
        transform.position = position;
        if (distance > 0.0001f && character != null && walkingAnimation != null)
        {
            walkedDistance += distance;
            ShowWalkingFrame(WalkingFrame(walkedDistance));
        }
        else StopWalking();
    }

    private void LateUpdate()
    {
        if (followCamera == null)
            return;
        var position = transform.position;
        position.z = followCamera.transform.position.z;
        if (map != null && followCamera.orthographic)
        {
            var bounds = map.bounds;
            float halfHeight = followCamera.orthographicSize;
            position.x = ClampToMap(position.x, bounds.min.x, bounds.max.x, halfHeight * followCamera.aspect);
            position.y = ClampToMap(position.y, bounds.min.y, bounds.max.y, halfHeight);
        }
        followCamera.transform.position = position;
    }

    private static float ClampToMap(float value, float minimum, float maximum, float margin)
    {
        return maximum - minimum <= margin * 2f
            ? (minimum + maximum) * 0.5f
            : Mathf.Clamp(value, minimum + margin, maximum - margin);
    }

    public void Configure(SpriteRenderer background, Camera camera)
    {
        map = background;
        followCamera = camera;
    }

    public static int WalkingFrame(float distance)
    {
        return Mathf.FloorToInt(Mathf.Max(0, distance) / 0.3f) % 2;
    }
    public Sprite WalkingSprite(int direction, int step) => walkingAnimation?.Frame(direction, step, RpgGame.Instance != null && RpgGame.Instance.State != null && RpgGame.Instance.State.heroIsBoy);

    private void StopWalking()
    {
        walkedDistance = 0;
        ShowWalkingFrame(0);
    }

    public static int FacingIndex(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x)) return direction.y > 0 ? 1 : 0;
        return direction.x < 0 ? 3 : direction.x > 0 ? 2 : 0;
    }

    public static float FacingAngle(Vector2 direction) =>
        direction.sqrMagnitude > 0.0001f ? Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg : 0f;

    private void ShowWalkingFrame(int step)
    {
        if (character == null || walkingAnimation == null) return;
        int orientation = FacingIndex(facing);
        character.sprite = walkingAnimation.Frame(orientation, step, RpgGame.Instance != null && RpgGame.Instance.State != null && RpgGame.Instance.State.heroIsBoy);
        character.flipX = false;
        transform.rotation = Quaternion.Euler(0, 0, FacingAngle(facing));
    }

    private void OnDestroy() { walkingAnimation?.Dispose(); }
}

// Preserve the supplied characters. Heading is controlled only by map-plane rotation.
public sealed class HeroWalkAnimation
{
    private readonly Sprite[,] frames = new Sprite[2, 2];
    public HeroWalkAnimation(Sprite standing)
    {
        frames[0,0] = Resources.Load<Sprite>("HeroUserStepA") ?? standing;
        frames[0,1] = Resources.Load<Sprite>("HeroUserStepB") ?? frames[0,0];
        frames[1,0] = Resources.Load<Sprite>("HeroBoyStepA") ?? standing;
        frames[1,1] = Resources.Load<Sprite>("HeroBoyStepB") ?? frames[1,0];
    }
    public Sprite Frame(int direction, int step, bool boy = false) => frames[boy ? 1 : 0, step == 1 ? 1 : 0];
    public void Dispose() { }
}
