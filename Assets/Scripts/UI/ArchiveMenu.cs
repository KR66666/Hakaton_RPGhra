using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Archive navigation. Category content will be connected later.</summary>
public sealed class ArchiveMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "MainMenu";

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

        PrepareCategory(canvas.transform, "Nepřátelé", 123);
        PrepareCategory(canvas.transform, "Lokace", 41);
        PrepareCategory(canvas.transform, "Předměty", -41);
        var back = MainMenu.Button(canvas.transform, "Zpět", -123, BackToMenu);

        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    private static void PrepareCategory(Transform parent, string title, float y)
    {
        var button = MainMenu.Button(parent, title, y, () => { });
        // Enable once this category has content and a click handler.
        button.interactable = false;
    }

    public void BackToMenu() => SceneManager.LoadScene(mainMenuScene);
}
