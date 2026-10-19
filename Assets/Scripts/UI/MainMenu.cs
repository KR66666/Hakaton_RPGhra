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
    public static bool LaunchTest;
    private RectTransform menuRoot;
    private GameObject locationsPanel;
    private Text heroSelection;
    private GameObject startPanel;
    private static RpgState pendingGame;
    private static bool pendingNewGame;
    public static void PrepareNewGame(bool boy,string name=null)
    {
        SelectedHeroIsBoy=boy;LaunchTest=false;pendingGame=RpgState.CreateNew(boy,name);pendingNewGame=true;
    }
    public static RpgState ConsumePendingGame(out bool newGame)
    {
        var state=pendingGame;newGame=pendingNewGame;
        pendingGame=null;pendingNewGame=false;return state;
    }

    private void Awake()
    {
        var canvas = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        menuRoot=canvas.GetComponent<RectTransform>();
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
        castle.color = Color.white; castle.raycastTarget = false;RpgVisualTheme.Picture(castle,false,4);
        var aspect = backdrop.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        aspect.aspectRatio = castle.sprite != null ? castle.sprite.rect.width / castle.sprite.rect.height : 1.5f;
        Label("Title", canvas.transform, "HAVRANOV", 72, new Vector2(0, 365), new Vector2(920, 96));
        Label("Subtitle", canvas.transform, "Tajemství snu • příběhové RPG", 26, new Vector2(0, 255), new Vector2(920, 40));
        Label("Tagline", canvas.transform, "Tři cykly. Jeden sen. Najdi cestu ven.", 24, new Vector2(0, -324), new Vector2(920, 40));
        Label("Controls", canvas.transform, "WASD • pohyb     J • tahový souboj     E • rozhovor     I • batoh", 22, new Vector2(0, -378), new Vector2(920, 36));
        Label("Save hint", canvas.transform, "Nová hra začíná od začátku. Uložený příběh otevři přes Načíst hru.", 20, new Vector2(0, -429), new Vector2(1100, 32));

        heroSelection = Label("Hero selection", canvas.transform, "Vzhled postavy nemění její vlastnosti.", 24, new Vector2(0, 180), new Vector2(920, 44));
        var play = Button(canvas.transform, "Hrát", 42, StartGame);
        Button(canvas.transform, "Lokace", -54, ShowLocations);
        Button(canvas.transform, "Archiv", -150, OpenArchive);
        Button(canvas.transform, "Konec", -246, Quit);

        var settings=Button(canvas.transform,"Nastavení",0,RpgSettings.Show);
        var sr=settings.GetComponent<RectTransform>();sr.anchorMin=sr.anchorMax=new Vector2(1,0);sr.pivot=new Vector2(1,0);sr.anchoredPosition=new Vector2(-30,28);sr.sizeDelta=new Vector2(260,64);
        var st=settings.GetComponentInChildren<Text>();st.rectTransform.sizeDelta=new Vector2(190,60);st.rectTransform.anchoredPosition=new Vector2(20,0);st.fontSize=29;
        var gear=Box("Settings gear",settings.transform,new Vector2(-94,0),new Vector2(34,34));var gi=gear.gameObject.AddComponent<Image>();gi.sprite=RpgSettings.GearSprite();gi.raycastTarget=false;
        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(play.gameObject);
    }

    public void StartGame()
    {
        var panel=BuildStartPanel("HRÁT");
        Label("Start choice",panel,"Začít nový příběh, nebo pokračovat v uloženém?",27,new Vector2(0,180),new Vector2(1100,70));
        Button(panel,"Nová hra",50,ShowNewCharacterChoice);
        Button(panel,"Načíst hru",-50,()=>{
            if(!RpgGame.TryReadStorySave(out var saved))
            {heroSelection.text="Uloženou hru se nepodařilo načíst.";startPanel.SetActive(false);return;}
            pendingGame=saved;pendingNewGame=false;SelectedHeroIsBoy=null;LaunchTest=false;RpgSettings.PlayKingdom();LoadScene(gameplayScene);
        });
        Button(panel,"Zpět",-150,()=>startPanel.SetActive(false));
    }
    private RectTransform BuildStartPanel(string title)
    {
        if(startPanel!=null){startPanel.SetActive(false);Destroy(startPanel);}
        var panel=Box("Start choice",menuRoot,Vector2.zero,Vector2.zero);
        panel.anchorMin=Vector2.zero;panel.anchorMax=Vector2.one;panel.offsetMin=panel.offsetMax=Vector2.zero;
        startPanel=panel.gameObject;RpgVisualTheme.Panel(startPanel.AddComponent<Image>());
        Label("Start title",panel,title,48,new Vector2(0,280),new Vector2(1000,80));return panel;
    }
    private void ShowNewCharacterChoice()
    {
        var panel=BuildStartPanel("NOVÁ HRA");
        Label("Choose appearance",panel,"Vyber vzhled hlavní postavy. Vlastnosti jsou stejné.",26,new Vector2(0,170),new Vector2(1100,90));
        Button(panel,"Muž",45,()=>ShowHeroName(true));
        Button(panel,"Žena",-55,()=>ShowHeroName(false));
        Button(panel,"Zpět",-155,StartGame);
    }
    private void ShowHeroName(bool boy)
    {
        RpgSettings.PlayKingdom();
        var panel=BuildStartPanel("JMÉNO POSTAVY");
        Label("Name prompt",panel,"Jak se bude tvá postava jmenovat?",28,new Vector2(0,175),new Vector2(1000,60));
        var fieldRect=Box("Jméno hrdiny",panel,new Vector2(0,65),new Vector2(560,80));
        var background=fieldRect.gameObject.AddComponent<Image>();
        background.sprite=PixelButtonSprite();background.type=Image.Type.Sliced;
        var input=fieldRect.gameObject.AddComponent<InputField>();input.targetGraphic=background;
        var text=Label("Name text",fieldRect,"",30,Vector2.zero,new Vector2(510,66));text.supportRichText=false;
        input.textComponent=text;input.characterLimit=24;input.lineType=InputField.LineType.SingleLine;
        var placeholder=Label("Name placeholder",fieldRect,boy?"Henri":"Rosalina",30,Vector2.zero,new Vector2(510,66));
        placeholder.color=new Color(.7f,.65f,.5f);input.placeholder=placeholder;
        Label("Name hint",panel,"Až 24 znaků. Prázdné pole ponechá výchozí jméno.",22,new Vector2(0,-10),new Vector2(1100,45));
        Button(panel,"Začít dobrodružství",-100,()=>{PrepareNewGame(boy,input.text);LoadScene(gameplayScene);});
        Button(panel,"Zpět",-200,ShowNewCharacterChoice);
        EventSystem.current.SetSelectedGameObject(input.gameObject);input.ActivateInputField();
    }
    public void ShowLocations()
    {
        if(locationsPanel!=null) {locationsPanel.SetActive(true);return;}
        var rect=Box("Locations",menuRoot,Vector2.zero,Vector2.zero);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;locationsPanel=rect.gameObject;
        var shade=locationsPanel.AddComponent<Image>();RpgVisualTheme.Panel(shade);
        Label("Locations title",rect,"LOKACE",54,new Vector2(0,290),new Vector2(1000,80));
        Label("Test description",rect,"TESTOVACÍ VERZE\nVšechna NPC, předměty, výbava a nepřátelé na jednom místě.\nTest začíná s vlastní výbavou a zlato ani postup z příběhu nemění.",25,new Vector2(0,140),new Vector2(1100,160));
        Button(rect,"Testovací verze",-10,()=>{pendingGame=null;pendingNewGame=false;LaunchTest=true;LoadScene(gameplayScene);});
        Button(rect,"Zpět",-120,()=>locationsPanel.SetActive(false));
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
    internal static Sprite PixelButtonSprite()
    {
        if (pixelButton != null) return pixelButton;
        var texture = new Texture2D(16,16,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp };
        var pixels = new Color32[256];
        for(int y=0;y<16;y++) for(int x=0;x<16;x++)
        {
            int edge=Mathf.Min(x,15-x,y,15-y);
            Color32 c = edge==0 ? new Color32(48,32,27,255) : edge==1 ? new Color32(248,203,115,255) :
                edge==2 ? new Color32(157,95,48,255) : new Color32(30,66,62,255);
            if(edge>2 && y>11) c=new Color32(46,87,76,255);
            if(edge>2 && y<4) c=new Color32(21,46,45,255);
            if((x<2||x>13)&&(y<2||y>13)) c=new Color32(0,0,0,0);
            pixels[y*16+x]=c;
        }
        texture.SetPixels32(pixels);texture.Apply(false,true);
        pixelButton=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(4,4,4,4));
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
        button.onClick.AddListener(RpgSettings.PlayEffect);
        button.onClick.AddListener(action);
        Label("Label", rect, title, 29, Vector2.zero, new Vector2(420, 76));
        return button;
    }

    internal static Text Label(string name, Transform parent, string content, int size, Vector2 position, Vector2 dimensions)
    {
        var text = Box(name, parent, position, dimensions).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = Mathf.RoundToInt(size*1.25f);
        text.rectTransform.sizeDelta=new Vector2(dimensions.x,dimensions.y*1.25f);
        text.color = RpgVisualTheme.Cream;
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
