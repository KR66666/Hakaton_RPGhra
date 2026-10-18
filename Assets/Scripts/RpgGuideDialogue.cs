using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgInterface
{
    private bool guideDialogueActive;
    private Coroutine guideReveal;
    private static Sprite guidePortraitSprite;
    private void ResetGuideDialogue()
    {
        if(guideReveal!=null){StopCoroutine(guideReveal);guideReveal=null;}
        guideDialogueActive=false;
    }
    private void BuildGuideDialogue()
    {
        guideDialogueActive=true;
        if(Application.isPlaying)
            guideReveal=StartCoroutine(RevealGuide(modalTitle.transform.parent.GetComponent<RectTransform>(),modalBody.text));
    }
    private IEnumerator RevealGuide(RectTransform box,string message)
    {
        Vector2 end=new Vector2(0,22);float elapsed=0;
        // Invisible remaining characters reserve their final line breaks while typing.
        modalBody.text="<color=#00000000>"+message+"</color>";
        while(elapsed<.45f && guideDialogueActive)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.45f),3);
            box.anchoredPosition=Vector2.Lerp(new Vector2(0,-box.sizeDelta.y-30),end,t);
            foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=false;
            yield return null;
        }
        box.anchoredPosition=end;
        int count=0;float characters=0;
        while(count<message.Length && guideDialogueActive)
        {
            characters+=Time.unscaledDeltaTime*55;count=Mathf.Min(message.Length,Mathf.FloorToInt(characters));
            modalBody.text=message.Substring(0,count)+"<color=#00000000>"+message.Substring(count)+"</color>";
            foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=false;
            yield return null;
        }
        modalBody.text=message;
        foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=true;
        guideReveal=null;
    }
    public void ShowGuideWelcome()
    {
        if(game.State.NpcDefeated("guard")){ShowFallenGuideNotes();return;}
        if(game.State.cycle==2){ShowCellarGuide();return;}
        Open("guideInstruction","PETR KŘÍŽ • RÁDCE","Vítej v Havranově! Jsem Petr Kříž. Tohle je tvůj příběh — prozkoumávej vesnici, mluv s lidmi a rozhoduj se po svém. Občas se ti ozvu s užitečnou radou.\n\n"+game.State.CellarObjective);
        NpcChallenge("guard");
        Action("Začít dobrodružství",()=>{
            if(game.State.currentArea==0 && game.State.cellarQuest==0)game.StartCellarQuest();else Close();
            game.State.guideSeenStage=game.State.cellarQuest;game.Save(false);
        });
    }
    public void ShowGuideInstruction()
    {
        if(game.State.NpcDefeated("guard")){ShowFallenGuideNotes();return;}
        int stage=game.State.cellarQuest;
        string message=stage==0?
            "Vítej v Havranově. Jmenuji se Petr Kříž a budu tvůj průvodce. Před cestou potřebujeme svíčky. Zeptej se dědy Matěje na náměstí — určitě poradí, kde je sehnat.":
            stage==1?"Nejdřív si promluv s dědou Matějem na náměstí. Přijdi k němu a stiskni E. Potom se ti ozvu s další radou.":
            stage==2?"Další stopa vede k Elišce. Dojdi ke dveřím jejího obchodu a zeptej se na svíčky.":
            "Svíčky máš. Vrať se ke mně u mostu a řekni mi o nich; vysvětlím ti rituál v kostele.";
        Open("guideInstruction","PETR KŘÍŽ • PRŮVODCE",message);
        NpcChallenge("guard");
        if(stage==0)Action("Rozumím • začít úkol",()=>{
            game.StartCellarQuest(); game.State.guideSeenStage=game.State.cellarQuest;game.Save(false);
        });
        else Action("Rozumím • pokračovat",Close);
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
