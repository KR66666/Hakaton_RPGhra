using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "SampleScene";
    [SerializeField] private string archiveScene = "Bestiář";
    private bool loading;

    private void Awake()
    {
        var canvas = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var play = Button(canvas.transform, "Hrát", 82, StartGame);
        Button(canvas.transform, "Archiv", 0, OpenArchive);
        Button(canvas.transform, "Konec", -82, Quit);

        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(play.gameObject);
    }

    public void StartGame()
    {
        LoadScene(gameplayScene);
    }

    public void OpenArchive()
    {
        LoadScene(archiveScene);
    }

    private void LoadScene(string scene)
    {
        if (loading) return;
        loading = true;
        SceneManager.LoadScene(scene);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    internal static Button Button(Transform parent, string title, float y, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(title, parent, new Vector2(0, y), new Vector2(400, 62));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.16f, 0.38f, 0.29f);
        colors.highlightedColor = new Color(0.22f, 0.53f, 0.38f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.12f, 0.28f, 0.21f);
        button.colors = colors;
        button.onClick.AddListener(action);
        Label("Label", rect, title, 25, Vector2.zero, new Vector2(380, 60));
        return button;
    }

    private static void Label(string name, Transform parent, string content, int size, Vector2 position, Vector2 dimensions)
    {
        var text = Box(name, parent, position, dimensions).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.color = new Color(0.92f, 0.94f, 0.98f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
    }

    private static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
