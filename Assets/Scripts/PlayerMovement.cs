using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField] private SpriteRenderer map;
    [SerializeField] private Camera followCamera;
    private SpriteRenderer character;
    private Sprite idleSprite, oppositeStep;
    private float walkedDistance;

    private void Start()
    {
        idleSprite = character != null ? character.sprite : null;
        oppositeStep = Resources.Load<Sprite>("HeroStepB");
    }

    private void Awake()
    {
        character = GetComponent<SpriteRenderer>();
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
        if (distance > 0.0001f && character != null && oppositeStep != null)
        {
            walkedDistance += distance;
            character.sprite = WalkingFrame(walkedDistance) == 0 ? idleSprite : oppositeStep;
            if (Mathf.Abs(direction.x) > 0.1f) character.flipX = direction.x < 0;
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

    public static int WalkingFrame(float distance) => Mathf.FloorToInt(distance / 0.4f) % 2;

    private void StopWalking()
    {
        walkedDistance = 0;
        if (character != null && idleSprite != null) character.sprite = idleSprite;
    }
}
