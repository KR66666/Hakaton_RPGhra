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
        transform.localScale = new Vector3(spriteScale.x * 0.90f, spriteScale.y * 0.90f, spriteScale.z);
        if (followCamera == null)
            followCamera = Camera.main;
        if(followCamera!=null && followCamera.orthographic)followCamera.orthographicSize=4.8f;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var game = RpgGame.Instance;
        if (game != null && game.IsEnteringRoom) return;
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
            ShowWalkingFrame(UsesDirectionalHero?DirectionalWalkingFrame(walkedDistance):WalkingFrame(walkedDistance));
        }
        else StopWalking();
    }

    private void LateUpdate()
    {
        if (followCamera == null)
            return;
        var position = transform.position+Vector3.up*1.8f;
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
    public bool UsesDirectionalHero => walkingAnimation!=null && walkingAnimation.HasDirectional && !(RpgGame.Instance?.State?.heroIsBoy ?? false);
    public static int DirectionalWalkingFrame(float distance) => 1+Mathf.FloorToInt(Mathf.Max(0,distance)/.3f)%4;
    public Sprite WalkingSprite(int direction, int step) => walkingAnimation?.Frame(direction, step, RpgGame.Instance != null && RpgGame.Instance.State != null && RpgGame.Instance.State.heroIsBoy);

    public void PlayEntranceStep(Vector2 direction,float distance)
    {
        if(direction.sqrMagnitude>.0001f) facing=direction.normalized;
        ShowWalkingFrame(UsesDirectionalHero?DirectionalWalkingFrame(distance):WalkingFrame(distance));
    }

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
        character.flipX = UsesDirectionalHero && orientation==2;
        transform.rotation = UsesDirectionalHero?Quaternion.identity:Quaternion.Euler(0, 0, FacingAngle(facing));
    }

    private void OnDestroy() { walkingAnimation?.Dispose(); }
}

// Supplied directional heroine frames; the existing boy keeps his original overhead artwork.
public sealed class HeroWalkAnimation
{
    [System.Serializable] public sealed class FrameDefinition { public float x,y,width,height,ppu; }
    [System.Serializable] public sealed class Sheet { public FrameDefinition[] frames; }
    private readonly Sprite[,] frames = new Sprite[2,3];
    private readonly System.Collections.Generic.List<Sprite> owned = new System.Collections.Generic.List<Sprite>();
    public HeroWalkAnimation(Sprite standing)
    {
        for(int hero=0;hero<2;hero++) for(int pose=0;pose<3;pose++)
            frames[hero,pose]=Resources.Load<Sprite>(hero==1?"HeroBoyStepA":"HeroUserStepA") ?? standing;
        var texture=Resources.Load<Texture2D>("HeroOverhead");
        var json=Resources.Load<TextAsset>("HeroOverhead");
        if(texture==null||json==null) return;
        var sheet=JsonUtility.FromJson<Sheet>(json.text);
        if(sheet?.frames==null||sheet.frames.Length!=6)return;
        for(int i=0;i<6;i++)
        {
            var f=sheet.frames[i];
            var sprite=Sprite.Create(texture,new Rect(f.x,f.y,f.width,f.height),Vector2.one*.5f,f.ppu);
            sprite.name=(i<3?"Girl":"Boy")+(i%3==0?" idle":i%3==1?" left foot":" right foot");
            frames[i/3,i%3]=sprite;owned.Add(sprite);
        }
    }
    public bool HasDirectional => RpgDirectionalHero.Available;
    public Sprite Frame(int direction,int step,bool boy=false) => !boy && HasDirectional ? RpgDirectionalHero.Frame(direction,step) : frames[boy?1:0,Mathf.Clamp(step,0,2)];
    public void Dispose()
    {
        foreach(var sprite in owned)
            if(Application.isPlaying) Object.Destroy(sprite); else Object.DestroyImmediate(sprite);
    }
}

