using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ArchiveMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene="MainMenu";
    private RpgState story;
    private Text content,section;
    private ScrollRect scroll;
    private void Awake()
    {
        if(RpgGame.TryReadStorySave(out var saved))
        {
            saved.Normalize();
            // Safe legacy-save inference: the current location and currently
            // owned items were definitely reached; infer no other locations.
            saved.RecordArchivePlace(RpgArchive.PlaceKey(saved.place));saved.RecordOwnedArchiveItems();
            story=saved;
        }
        var canvas=new GameObject("Archive Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var backdrop=Box(canvas.transform,"Pozadí kroniky",Vector2.zero,Vector2.zero);
        backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
        RpgVisualTheme.Panel(backdrop.gameObject.AddComponent<Image>());
        var title=TextAt(canvas.transform,"KRONIKA HAVRANOVA",new Vector2(70,-45),new Vector2(1460,75),48);
        title.alignment=TextAnchor.MiddleCenter;
        TextAt(canvas.transform,story==null?"Zatím nemáš rozehranou hru.":"Záznamy z uložené hry • "+story.HeroName,new Vector2(70,-124),new Vector2(1460,36),24).alignment=TextAnchor.MiddleCenter;
        string[] categories={"Postavy","Nepřátelé","Lokace","Předměty"};
        for(int i=0;i<categories.Length;i++)
        {
            int category=i;
            var button=MainMenu.Button(canvas.transform,categories[i],0,()=>ShowCategory(category,categories[category]));
            Place(button.GetComponent<RectTransform>(),new Vector2(70,-200-i*90),new Vector2(300,64));
            var caption=button.GetComponentInChildren<Text>();caption.rectTransform.sizeDelta=new Vector2(276,60);caption.fontSize=28;
        }
        var panel=Box(canvas.transform,"Obsah archivu",new Vector2(420,-184),new Vector2(1110,580));
        RpgVisualTheme.Panel(panel.gameObject.AddComponent<Image>());
        section=TextAt(panel,"",new Vector2(28,-20),new Vector2(1054,45),32);
        var viewport=Box(panel,"Posuvný obsah",new Vector2(28,-86),new Vector2(1054,466));
        viewport.gameObject.AddComponent<Image>().color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
        scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
        content=TextAt(viewport,"",Vector2.zero,new Vector2(1028,466),26);content.lineSpacing=1.2f;
        content.rectTransform.anchorMax=new Vector2(1,1);content.rectTransform.sizeDelta=new Vector2(-24,0);
        var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=content.rectTransform;
        var rail=Box(viewport,"Posuvník",new Vector2(1038,0),new Vector2(16,466));rail.gameObject.AddComponent<Image>().color=new Color(.1f,.075f,.05f);
        var bar=rail.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;
        var handle=Box(rail,"Úchyt",Vector2.zero,Vector2.zero);handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
        var handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=RpgVisualTheme.Gold;bar.handleRect=handle;bar.targetGraphic=handleImage;
        scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        var back=MainMenu.Button(canvas.transform,"Zpět",0,BackToMenu);Place(back.GetComponent<RectTransform>(),new Vector2(650,-802),new Vector2(300,64));
        back.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(276,60);back.GetComponentInChildren<Text>().fontSize=28;
        ShowCategory(0,categories[0]);
        if(EventSystem.current==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }
    private void ShowCategory(int category,string title)
    {
        var entries=RpgArchive.Entries(story,category);section.text=title.ToUpperInvariant()+" • "+entries.Count;
        content.text=entries.Count==0?(story==null?"Archiv je prázdný. Začni novou hru a objevuj svět.":"V této kategorii zatím nemáš žádné objevené záznamy."):string.Join("\n\n",entries);
        Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
    }
    private static void Place(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;}
    private static RectTransform Box(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);Place(rect,position,size);return rect;
    }
    private static Text TextAt(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
    {
        var label=Box(parent,"Text",position,size).gameObject.AddComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text=text;label.fontSize=fontSize;label.color=RpgVisualTheme.Cream;label.alignment=TextAnchor.UpperLeft;
        label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;label.raycastTarget=false;return label;
    }
    public void BackToMenu()=>SceneManager.LoadScene(mainMenuScene);
}
