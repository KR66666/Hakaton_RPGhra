using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Reference pages for the playable village adventure.</summary>
public sealed class ArchiveMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MainMenu";
    private Text content;

    private void Awake()
    {
        var canvas = new GameObject("Archive Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var backdrop=new GameObject("Pixelový rám kroniky",typeof(RectTransform),typeof(Image));
        backdrop.transform.SetParent(canvas.transform,false);var br=backdrop.GetComponent<RectTransform>();
        br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
        RpgVisualTheme.Panel(backdrop.GetComponent<Image>());
        MainMenu.Label("Title", canvas.transform, "KRONIKA HAVRANOVA", 44, new Vector2(0, 360), new Vector2(1200, 80));
        content = MainMenu.Label("Content", canvas.transform, "", 27, new Vector2(230, 30), new Vector2(850, 540));
        content.alignment = TextAnchor.UpperLeft;
        PrepareCategory(canvas.transform, "Nepřátelé", 160, Enemies);
        PrepareCategory(canvas.transform, "Lokace", 60, Locations);
        PrepareCategory(canvas.transform, "Předměty", -40, Items);
        var back = MainMenu.Button(canvas.transform, "Zpět", -360, BackToMenu);
        Enemies();

        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    private void PrepareCategory(Transform parent, string title, float y, UnityEngine.Events.UnityAction action)
    {
        var button = MainMenu.Button(parent, title, y, action);
        button.GetComponent<RectTransform>().anchoredPosition = new Vector2(-570, y);
    }

    private void Enemies() => content.text = "NÁJEZDNÍK\nSlabší bojovník s 48 zdraví. Souboj je tahový. Vyber útok, krytí, lektvar nebo útěk. Odměna: 12 zlata.\n\nVELITEL VRANEK\nObjeví se po porážce pěti nájezdníků. Má 220 zdraví a silnější, pomalejší úder. Připrav si lektvary a lepší meč.\n\nJ: zahájit tahový souboj. Shift: běh. H: lektvar.";
    private void Locations() => content.text = "HAVRANOV\nStředověká vesnice s tržištěm, hradem a řekou.\n\nNÁMĚSTÍ\nRychtář Radovan zadává hlavní úkol. Obchodnice Eliška prodává lektvary a vylepšuje vybavení. Bylinkářka Mira léčí a vyrábí lektvary.\n\nSEVERNÍ BRÁNA\nÚkryt velitele Vranka. Nejdříve poraz jeho nájezdníky.\n\nZlaté značky: truhly. Zelené značky: byliny.";
    private void Items() => content.text = "ŽELEZNÝ MEČ\nZákladní útok 18. Každé vylepšení přidá 9 poškození.\n\nKOŽENÁ ZBROJ\nKaždé vylepšení snižuje příchozí zranění o 3.\n\nLÉČIVÝ LEKTVAR\nObnoví 60 zdraví. Cena: 15 zlata. Použití: H.\n\nLÉČIVÉ BYLINY\nDvě byliny Mira přemění na jeden lektvar.\n\nTRUHLA\nObsahuje 30 zlata a léčivý lektvar.";

    public void BackToMenu() => SceneManager.LoadScene(mainMenuScene);
}
