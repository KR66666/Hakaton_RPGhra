using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgInterface
{
    private bool guideDialogueActive;
    private Coroutine guideReveal;
    private RectTransform guideDecoration;
    private Color previousGuidePanelColor;
    private Sprite previousGuidePanelSprite;
    private Image.Type previousGuidePanelType;
    private static Sprite guidePortraitSprite;

    private void ResetGuideDialogue()
    {
        if(!guideDialogueActive)return;
        if(guideReveal!=null){StopCoroutine(guideReveal);guideReveal=null;}
        guideDialogueActive=false;
        if(guideDecoration!=null){guideDecoration.gameObject.SetActive(false);Destroy(guideDecoration.gameObject);guideDecoration=null;}
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        Anchor(box,new Vector2(.5f,.5f),Vector2.zero);box.sizeDelta=new Vector2(760,650);
        var panel=box.GetComponent<Image>();panel.color=previousGuidePanelColor;panel.sprite=previousGuidePanelSprite;panel.type=previousGuidePanelType;
        var outline=box.GetComponent<Outline>();if(outline!=null)outline.enabled=true;
        modalTitle.rectTransform.anchoredPosition=new Vector2(32,-22);modalTitle.rectTransform.sizeDelta=new Vector2(650,45);modalTitle.fontSize=30;
        modalBody.lineSpacing=1f;
        modalBody.rectTransform.anchoredPosition=new Vector2(32,-85);modalBody.rectTransform.sizeDelta=new Vector2(696,290);
        actions.anchoredPosition=new Vector2(32,-378);actions.sizeDelta=new Vector2(696,245);
        modal.GetComponent<Image>().color=new Color(0,0,0,.68f);
    }
    private void BuildGuideDialogue()
    {
        guideDialogueActive=true;
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        var panel=box.GetComponent<Image>();previousGuidePanelColor=panel.color;previousGuidePanelSprite=panel.sprite;previousGuidePanelType=panel.type;
        Anchor(box,new Vector2(.5f,0),new Vector2(0,22));box.sizeDelta=new Vector2(1480,244);
        panel.sprite=DialogueFrame();panel.type=Image.Type.Sliced;panel.color=Color.white;
        var outline=box.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
        modal.GetComponent<Image>().color=new Color(0,0,0,.18f);
        guideDecoration=Rect(modal,"Petr • velký portrét před zadáním",Vector2.zero,Vector2.zero);
        guideDecoration.anchorMin=Vector2.zero;guideDecoration.anchorMax=Vector2.one;
        guideDecoration.offsetMin=guideDecoration.offsetMax=Vector2.zero;
        guideDecoration.SetAsFirstSibling();
        var portrait=Rect(guideDecoration,"Petr Kříž • dodaný portrét",Vector2.zero,Vector2.zero);
        portrait.anchorMin=new Vector2(0,0);portrait.anchorMax=new Vector2(0,1);
        portrait.offsetMin=new Vector2(40,260);portrait.offsetMax=new Vector2(620,-30);
        var portraitImage=portrait.gameObject.AddComponent<Image>();portraitImage.sprite=GuidePortrait();portraitImage.preserveAspect=true;portraitImage.raycastTarget=false;RpgVisualTheme.Picture(portraitImage,false,5);
        modalTitle.rectTransform.anchoredPosition=new Vector2(32,-20);modalTitle.rectTransform.sizeDelta=new Vector2(980,40);modalTitle.fontSize=28;
        modalTitle.text="PETR KŘÍŽ  •  PRŮVODCE";
        modalBody.rectTransform.anchoredPosition=new Vector2(32,-76);modalBody.rectTransform.sizeDelta=new Vector2(980,145);modalBody.fontSize=24;modalBody.lineSpacing=1.15f;
        actions.anchoredPosition=new Vector2(1045,-24);actions.sizeDelta=new Vector2(400,210);
        guideReveal=StartCoroutine(RevealGuide(box,modalBody.text));
    }
    private IEnumerator RevealGuide(RectTransform box,string message)
    {
        Vector2 end=new Vector2(0,22);float elapsed=0;
        modalBody.text="";
        while(elapsed<.45f && guideDialogueActive)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.45f),3);
            box.anchoredPosition=Vector2.Lerp(new Vector2(0,-270),end,t);
            if(guideDecoration!=null)guideDecoration.anchoredPosition=new Vector2(Mathf.Lerp(-650,0,t),0);
            foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=false;
            yield return null;
        }
        box.anchoredPosition=end;
        int count=0;float characters=0;
        while(count<message.Length && guideDialogueActive)
        {
            characters+=Time.unscaledDeltaTime*55;count=Mathf.Min(message.Length,Mathf.FloorToInt(characters));
            modalBody.text=message.Substring(0,count);
            foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=false;
            yield return null;
        }
        modalBody.text=message;
        foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=true;
        guideReveal=null;
    }
    public void ShowGuideWelcome()
    {
        Open("guideInstruction","PETR KŘÍŽ • RÁDCE","Vítej v Havranově! Jsem Petr Kříž. Tohle je tvůj příběh — prozkoumávej vesnici, mluv s lidmi a rozhoduj se po svém. Občas se ti ozvu s užitečnou radou.\n\n"+game.State.CellarObjective);
        Action("Začít dobrodružství",()=>{
            if(game.State.currentArea==0 && game.State.cellarQuest==0)game.StartCellarQuest();else Close();
            game.State.guideSeenStage=game.State.cellarQuest;game.Save(false);
        });
    }
    public void ShowGuideInstruction()
    {
        int stage=game.State.cellarQuest;
        string message=stage==0?
            "Vítej v Havranově. Jmenuji se Petr Kříž a budu tvůj průvodce. Před cestou potřebujeme svíčky. Zeptej se dědy Matěje na náměstí — určitě poradí, kde je sehnat.":
            stage==1?"Nejdřív si promluv s dědou Matějem na náměstí. Přijdi k němu a stiskni E. Potom se ti ozvu s další radou.":
            stage==2?"Matěj nás poslal k Elišce. Dojdi ke dveřím jejího obchodu a vejdi dovnitř. Než půjdeme do sklepa, slušně se jí zeptej na svíčky.":
            "Ten vzkaz si dobře schovej. Značka havrana a východní most nám ukazují další cestu. Teď mi lístek ukaž a společně se připravíme na výpravu.";
        Open("guideInstruction","PETR KŘÍŽ • PRŮVODCE",message);
        if(stage==0)Action("Rozumím • začít úkol",()=>{
            game.StartCellarQuest(); game.State.guideSeenStage=game.State.cellarQuest;game.Save(false);
        });
        else Action("Rozumím • pokračovat",Close);
    }
    private static Sprite DialogueFrame()
    {
        return RpgVisualTheme.Frame;
    }
    private static Sprite GuidePortrait()
    {
        if(guidePortraitSprite!=null)return guidePortraitSprite;
        var source=Resources.Load<Texture2D>("PetrKrizPortrait");if(source==null)return null;
        // Only the black backdrop connected to the image edge is transparent.
        // Enclosed dark clothing and facial pixels retain their original colors.
        int width=source.width,height=source.height;var pixels=source.GetPixels32();
        var visited=new bool[pixels.Length];var queue=new Queue<int>();
        System.Action<int> seed=index=>{
            if(visited[index])return;visited[index]=true;
            var c=pixels[index];if(c.r>10||c.g>10||c.b>10)return;
            pixels[index].a=0;queue.Enqueue(index);
        };
        for(int x=0;x<width;x++){seed(x);seed((height-1)*width+x);}
        for(int y=0;y<height;y++){seed(y*width);seed(y*width+width-1);}
        while(queue.Count>0)
        {
            int index=queue.Dequeue(),x=index%width,y=index/width;
            if(x>0)seed(index-1);if(x<width-1)seed(index+1);
            if(y>0)seed(index-width);if(y<height-1)seed(index+width);
        }
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels32(pixels);texture.Apply(false,true);
        guidePortraitSprite=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,0),100);
        guidePortraitSprite.name="Petr Kříž • portrét bez okolního pozadí";return guidePortraitSprite;
    }
}
