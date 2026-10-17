using UnityEngine;
using UnityEngine.UI;

/// <summary>Simple screen-space HUD. Gameplay scripts can call SetHealth and SetStamina.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHud : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float health = 100f;
    [Header("Stamina")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField, Min(0f)] private float stamina = 100f;

    private RectTransform healthFill;
    private RectTransform staminaFill;
    private Text healthValue;
    private Text staminaValue;

    public float Health => health;
    public float Stamina => stamina;

    private void Awake()
    {
        var canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = Rect("Vitals", canvasObject.transform, new Vector2(32, -32), new Vector2(340, 136));
        var background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.065f, 0.92f);
        background.raycastTarget = false;
        CreateBar(panel, "HP", 18, new Color(0.88f, 0.22f, 0.28f), out healthFill, out healthValue);
        CreateBar(panel, "STAMINA", 76, new Color(0.28f, 0.8f, 0.48f), out staminaFill, out staminaValue);
        Refresh();
    }

    private void Update() => Refresh();

    public void SetHealth(float current, float maximum)
    {
        maxHealth = Mathf.Max(1f, maximum);
        health = Mathf.Clamp(current, 0f, maxHealth);
        Refresh();
    }

    public void SetStamina(float current, float maximum)
    {
        maxStamina = Mathf.Max(1f, maximum);
        stamina = Mathf.Clamp(current, 0f, maxStamina);
        Refresh();
    }

    private void Refresh()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        maxStamina = Mathf.Max(1f, maxStamina);
        health = Mathf.Clamp(health, 0f, maxHealth);
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
        if (healthFill == null || staminaFill == null) return;
        healthFill.anchorMax = new Vector2(health / maxHealth, 1f);
        staminaFill.anchorMax = new Vector2(stamina / maxStamina, 1f);
        healthValue.text = $"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}";
        staminaValue.text = $"{Mathf.CeilToInt(stamina)} / {Mathf.CeilToInt(maxStamina)}";
    }

    private static void CreateBar(Transform parent, string label, float top, Color color,
        out RectTransform fill, out Text value)
    {
        var title = Label(label, parent, new Vector2(18, -top), new Vector2(140, 22));
        title.text = label;
        title.color = color;
        value = Label(label + " Value", parent, new Vector2(158, -top), new Vector2(164, 22));
        value.alignment = TextAnchor.MiddleRight;
        var track = Rect(label + " Track", parent, new Vector2(18, -top - 28), new Vector2(304, 12));
        var trackImage = track.gameObject.AddComponent<Image>();
        trackImage.color = new Color(0.12f, 0.15f, 0.19f);
        trackImage.raycastTarget = false;
        fill = Rect(label + " Fill", track, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = color;
        fillImage.raycastTarget = false;
    }

    private static Text Label(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 17;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.92f, 0.94f, 0.98f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
