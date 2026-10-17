using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField] private SpriteRenderer map;
    [SerializeField] private Camera followCamera;
    private SpriteRenderer character;

    private void Awake()
    {
        character = GetComponent<SpriteRenderer>();
        if (followCamera == null)
            followCamera = Camera.main;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !Application.isFocused)
            return;

        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
        var direction = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        var position = transform.position + (Vector3)(direction * (moveSpeed * Time.deltaTime));
        if (map != null)
        {
            var bounds = map.bounds;
            var margin = character != null ? character.bounds.extents : Vector3.zero;
            position.x = ClampToMap(position.x, bounds.min.x, bounds.max.x, margin.x);
            position.y = ClampToMap(position.y, bounds.min.y, bounds.max.y, margin.y);
        }
        transform.position = position;
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
}
