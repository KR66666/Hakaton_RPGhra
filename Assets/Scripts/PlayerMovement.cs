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
        transform.localScale *= 0.85f;
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
        int phase = Mathf.FloorToInt(Mathf.Max(0, distance) / 0.2f) % 4;
        return phase == 0 ? 1 : phase == 2 ? 2 : 0;
    }
    public Sprite WalkingSprite(int direction, int step) => walkingAnimation?.Frame(direction, step);

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

    private void ShowWalkingFrame(int step)
    {
        if (character == null || walkingAnimation == null) return;
        int orientation = FacingIndex(facing);
        character.sprite = walkingAnimation.Frame(orientation, step);
        character.flipX = false;
    }

    private void OnDestroy() { walkingAnimation?.Dispose(); }
}

public sealed class HeroWalkAnimation
{
    [System.Serializable]
    public sealed class FrameDefinition
    {
        public float x, y, width, height, ppu, pivotX, pivotY;
    }
    [System.Serializable]
    public sealed class SheetDefinition { public FrameDefinition[] frames; }
    private readonly Sprite[,] poses = new Sprite[4, 3];
    private readonly System.Collections.Generic.List<Sprite> generated = new System.Collections.Generic.List<Sprite>();
    public HeroWalkAnimation(Sprite standing)
    {
        for (int direction = 0; direction < 4; direction++)
            for (int pose = 0; pose < 3; pose++) poses[direction, pose] = standing;
        var texture = Resources.Load<Texture2D>("HeroDirections");
        var definitions = Resources.Load<TextAsset>("HeroDirections");
        if (texture == null || definitions == null) return;
        var sheet = JsonUtility.FromJson<SheetDefinition>(definitions.text);
        if (sheet?.frames == null || sheet.frames.Length != 12) return;
        string[] names = { "South", "North", "East", "West" };
        string[] states = { "Idle", "Left foot", "Right foot" };
        for (int i = 0; i < 12; i++)
        {
            var frame = sheet.frames[i];
            var sprite = Sprite.Create(texture, new Rect(frame.x, frame.y, frame.width, frame.height),
                new Vector2(frame.pivotX, frame.pivotY), frame.ppu, 0, SpriteMeshType.FullRect);
            sprite.name = names[i / 3] + " " + states[i % 3];
            generated.Add(sprite);
            poses[i / 3, i % 3] = sprite;
        }
    }
    public Sprite Frame(int direction, int step) => poses[Mathf.Clamp(direction, 0, 3), Mathf.Clamp(step, 0, 2)];

    public void Dispose()
    {
        foreach (var sprite in generated) Release(sprite);
    }

    private static void Release(Sprite sprite)
    {
        if (sprite == null) return;
        if (Application.isPlaying) Object.Destroy(sprite);
        else Object.DestroyImmediate(sprite);
    }
}

