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
    public static bool? SelectedHeroIsBoy;
    private Text heroSelection;

    private void SelectHero(bool boy)
    {
        SelectedHeroIsBoy = boy;
        heroSelection.text = boy ? "Vybraný hrdina: KLUK" : "Vybraná hrdinka: HOLKA";
    }

    private void Awake()
    {
        var canvas = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().pixelPerfect = true;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(canvas.transform, false);
        var backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero; backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        var castle = backdrop.GetComponent<Image>();
        castle.sprite = Resources.Load<Sprite>("MainMenuCastle");
        castle.color = Color.white; castle.raycastTarget = false;
        var aspect = backdrop.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        aspect.aspectRatio = castle.sprite != null ? castle.sprite.rect.width / castle.sprite.rect.height : 1.5f;
        Label("Title", canvas.transform, "HAVRANOV", 72, new Vector2(0, 350), new Vector2(920, 96));
        Label("Subtitle", canvas.transform, "Strážce vesnice • středověké RPG", 26, new Vector2(0, 278), new Vector2(920, 40));
        Label("Tagline", canvas.transform, "Meč, odvaha a vesnice, která potřebuje hrdinu.", 24, new Vector2(0, -292), new Vector2(920, 40));
        Label("Controls", canvas.transform, "WASD • pohyb     J • útok     E • rozhovor     I • batoh", 22, new Vector2(0, -346), new Vector2(920, 36));
        Label("Save hint", canvas.transform, "Uložený příběh se při vstupu automaticky načte.", 20, new Vector2(0, -388), new Vector2(920, 32));

        heroSelection = Label("Hero selection", canvas.transform, "Vyber hlavního hrdinu", 26, new Vector2(0, 210), new Vector2(920, 44));
        var girl = Button(canvas.transform, "Holka", 0, () => SelectHero(false));
        girl.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, 134);
        var boy = Button(canvas.transform, "Kluk", 0, () => SelectHero(true));
        boy.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, 134);
        var play = Button(canvas.transform, "Hrát", 22, StartGame);
        Button(canvas.transform, "Archiv", -82, OpenArchive);
        Button(canvas.transform, "Konec", -186, Quit);

        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(play.gameObject);
    }

    public void StartGame()
    {
        if (!SelectedHeroIsBoy.HasValue) { heroSelection.text = "Nejprve vyber: Holka nebo Kluk"; return; }
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

    private static Sprite pixelButton;
    private static Sprite PixelButtonSprite()
    {
        if (pixelButton != null) return pixelButton;
        var texture = new Texture2D(16,16,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp };
        var pixels = new Color32[256];
        for(int y=0;y<16;y++) for(int x=0;x<16;x++)
        {
            int edge=Mathf.Min(x,15-x,y,15-y);
            Color32 c = edge==0 ? new Color32(24,19,22,255) : edge==1 ? new Color32(180,139,74,255) :
                edge==2 ? new Color32(84,57,39,255) : new Color32(55,49,43,255);
            if(edge>2 && y>11) c=new Color32(73,63,48,255);
            if(edge>2 && y<4) c=new Color32(38,33,32,255);
            if((x<2||x>13)&&(y<2||y>13)) c=new Color32(0,0,0,0);
            pixels[y*16+x]=c;
        }
        texture.SetPixels32(pixels);texture.Apply(false,true);
        pixelButton=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16,0,SpriteMeshType.FullRect,new Vector4(4,4,4,4));
        pixelButton.name="16px medieval button";return pixelButton;
    }

    internal static Button Button(Transform parent, string title, float y, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(title, parent, new Vector2(0, y), new Vector2(440, 80));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = PixelButtonSprite(); image.type = Image.Type.Sliced;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.15f, 1.05f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.65f, .65f, .7f);
        colors.fadeDuration = 0;
        button.colors = colors;
        button.onClick.AddListener(action);
        Label("Label", rect, title, 29, Vector2.zero, new Vector2(420, 76));
        return button;
    }

    internal static Text Label(string name, Transform parent, string content, int size, Vector2 position, Vector2 dimensions)
    {
        var text = Box(name, parent, position, dimensions).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.color = new Color(0.92f, 0.94f, 0.98f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0,0,0,.9f); shadow.effectDistance = new Vector2(2,-2);
        return text;
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
