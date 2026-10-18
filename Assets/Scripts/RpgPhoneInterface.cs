using UnityEngine;
using UnityEngine.UI;

public static class RpgHudPreferences
{
    public static bool Visible(string section) => PlayerPrefs.GetInt("rpg.hud."+section,1)==1;
    public static void SetVisible(string section,bool visible)
    {
        PlayerPrefs.SetInt("rpg.hud."+section,visible?1:0);PlayerPrefs.Save();
    }
}

public sealed partial class RpgInterface
{
    private RectTransform phoneChrome;
    private static Sprite phoneShell;
    private RectTransform controlsWidget,questWidget,mapWidget,characterWidget;
    private bool PhoneOpen => currentModal=="inventory" || currentModal=="journal" || currentModal=="cellarJournal";
    private void UpdateHudVisibility()
    {
        if(controlsWidget!=null)controlsWidget.gameObject.SetActive(RpgHudPreferences.Visible("controls"));
        if(combatWidgets!=null)foreach(var widget in combatWidgets)if(widget!=null)widget.gameObject.SetActive(RpgHudPreferences.Visible("controls"));
        if(questWidget!=null)questWidget.gameObject.SetActive(RpgHudPreferences.Visible("quest"));
        bool navigation=RpgHudPreferences.Visible("navigation");
        if(mapWidget!=null)mapWidget.gameObject.SetActive(navigation);
        if(characterWidget!=null)characterWidget.gameObject.SetActive(navigation);
    }
    public void ToggleHudExtras()
    {
        bool visible=!(RpgHudPreferences.Visible("controls") || RpgHudPreferences.Visible("quest") || RpgHudPreferences.Visible("navigation"));
        foreach(var section in new[]{"controls","quest","navigation"})RpgHudPreferences.SetVisible(section,visible);
        UpdateHudVisibility();game.Notify(visible?"Panely zobrazeny. F2 je znovu skryje.":"Panely skryty. F2 je znovu zobrazí. Mobil: I / Q.");
    }
    private void ApplyPhoneLayout()
    {
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        Anchor(box,new Vector2(.5f,.5f),Vector2.zero);box.sizeDelta=new Vector2(600,840);
        var background=box.GetComponent<Image>();background.sprite=PhoneShell();background.type=Image.Type.Sliced;background.color=new Color(.025f,.035f,.05f);
        var outline=box.GetComponent<Outline>();outline.enabled=true;outline.effectColor=new Color(.3f,.34f,.4f);outline.effectDistance=new Vector2(5,-5);
        modal.GetComponent<Image>().color=new Color(0,0,0,.65f);
        phoneChrome=Rect(box,"Mobil hlavní postavy",Vector2.zero,Vector2.zero);
        phoneChrome.anchorMin=Vector2.zero;phoneChrome.anchorMax=Vector2.one;phoneChrome.offsetMin=phoneChrome.offsetMax=Vector2.zero;phoneChrome.SetAsFirstSibling();
        var status=Label(phoneChrome,"OSOBNÍ MOBIL                         OFFLINE",new Vector2(32,-20),new Vector2(536,28),17,new Color(.53f,.72f,.77f));
        var speaker=Rect(phoneChrome,"Reproduktor",new Vector2(255,-8),new Vector2(90,4));speaker.gameObject.AddComponent<Image>().color=new Color(.25f,.29f,.33f);
        PlaceDialogueRect(modalTitle.rectTransform,new Rect(32,58,536,48));modalTitle.fontSize=30;modalTitle.color=new Color(.55f,.88f,.88f);
        if(currentModal=="inventory")modalTitle.text="INVENTÁŘ";
        else if(currentModal=="journal")modalTitle.text="ÚKOLY";
        else {modalTitle.text="SVÍČKY A RITUÁL";modalTitle.fontSize=28;}
        PhoneTab("Inventář [I]",new Rect(32,116,260,42),()=>{if(currentModal!="inventory")ShowInventory();},currentModal=="inventory");
        PhoneTab("Úkoly [Q]",new Rect(308,116,260,42),()=>{if(currentModal!="journal")ShowPhoneJournal();},currentModal!="inventory");
        ApplyScrollableBody(new Rect(32,178,536,360),26);
        modalBody.color=new Color(.85f,.89f,.94f);
        PlaceDialogueRect(actions,new Rect(32,560,536,214));
        var home=Rect(phoneChrome,"Domovská lišta",new Vector2(240,-818),new Vector2(120,5));home.gameObject.AddComponent<Image>().color=new Color(.65f,.72f,.78f);
        var caption=Label(phoneChrome,"I / Q • aplikace     Esc • odložit mobil",new Vector2(32,-786),new Vector2(536,25),18,new Color(.53f,.64f,.7f));caption.alignment=TextAnchor.MiddleCenter;
    }
    private void PhoneTab(string title,Rect area,UnityEngine.Events.UnityAction callback,bool selected)
    {
        var rect=Rect(phoneChrome,title,Vector2.zero,Vector2.zero);PlaceDialogueRect(rect,area);
        var image=rect.gameObject.AddComponent<Image>();image.color=selected?new Color(.045f,.075f,.11f):new Color(.10f,.30f,.34f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(callback);
        var text=Label(rect,title,new Vector2(8,0),new Vector2(area.width-16,area.height),22,Color.white);text.alignment=TextAnchor.MiddleCenter;
    }
    private static Sprite PhoneShell()
    {
        if(phoneShell!=null)return phoneShell;
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color32[64*64];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float dx=Mathf.Max(14-x,x-49),dy=Mathf.Max(14-y,y-49);
            if(dx<=0 || dy<=0 || dx*dx+dy*dy<=196)pixels[y*64+x]=new Color32(255,255,255,255);
        }
        texture.SetPixels32(pixels);texture.Apply(false,true);
        phoneShell=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(15,15,15,15));
        return phoneShell;
    }
}
