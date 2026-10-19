using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgInterface
{
    private bool guideDialogueActive;
    private Coroutine guideReveal;
    private bool guideSkipRequested;
    private int guideOpenedFrame;
    private static Sprite guidePortraitSprite;
    private void ResetGuideDialogue()
    {
        if(guideReveal!=null){StopCoroutine(guideReveal);guideReveal=null;}
        guideDialogueActive=false;
        guideSkipRequested=false;
    }
    private void BuildGuideDialogue()
    {
        guideDialogueActive=true;
        guideSkipRequested=false;guideOpenedFrame=Time.frameCount;
        if(Application.isPlaying)
            guideReveal=StartCoroutine(RevealGuide(modalTitle.transform.parent.GetComponent<RectTransform>(),modalBody.text));
    }
    private void UpdateGuideSkip()
    {
        if(!guideDialogueActive || guideReveal==null || Time.frameCount<=guideOpenedFrame)return;
        var mouse=UnityEngine.InputSystem.Mouse.current;
        if(mouse!=null && mouse.leftButton.wasPressedThisFrame)guideSkipRequested=true;
    }
    private IEnumerator RevealGuide(RectTransform box,string message)
    {
        Vector2 end=new Vector2(0,22);float elapsed=0;
        // Invisible remaining characters reserve their final line breaks while typing.
        modalBody.text="<color=#00000000>"+message+"</color>";
        while(elapsed<.45f && guideDialogueActive && !guideSkipRequested)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.45f),3);
            box.anchoredPosition=Vector2.Lerp(new Vector2(0,-box.sizeDelta.y-30),end,t);
            foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=false;
            yield return null;
        }
        box.anchoredPosition=end;
        int count=0;float characters=0;
        while(count<message.Length && guideDialogueActive && !guideSkipRequested)
        {
            characters+=Time.unscaledDeltaTime*55;count=Mathf.Min(message.Length,Mathf.FloorToInt(characters));
            modalBody.text=message.Substring(0,count)+"<color=#00000000>"+message.Substring(count)+"</color>";
            foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=false;
            yield return null;
        }
        modalBody.text=message;
        // Consume the revealing click so its release cannot also activate a
        // dialogue choice underneath the pointer.
        if(guideSkipRequested)
        {
            while(UnityEngine.InputSystem.Mouse.current!=null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)yield return null;
            yield return null;
        }
        foreach(var button in actions.GetComponentsInChildren<Button>())button.interactable=true;
        guideReveal=null;
    }
    public void ShowGuideWelcome()
    {
        if(game.State.cycle>=2)return;
        if(game.State.NpcDefeated("guard")){ShowFallenGuideNotes();return;}
        if(FirstMeeting("guard","Petr Kříž • průvodce","guideInstruction",ShowGuideWelcome))return;
        Open("guideInstruction","PETR KŘÍŽ • RÁDCE","Tak tedy, "+game.State.HeroName+". Jestli hledáš cestu z toho podivného snu, místní pověsti mluví o rituálu se svíčkami. Ptej se obyvatel — oni zdejší místa znají lépe než ty.\n\n"+game.State.CellarObjective);
        NpcChallenge("guard");
        Action("Začít dobrodružství",()=>{
            if(game.State.currentArea==0 && game.State.cellarQuest==0)game.StartCellarQuest();else Close();
            game.State.guideSeenStage=game.State.cellarQuest;game.Save(false);
        });
    }
    public void ShowGuideInstruction()
    {
        if(game.State.cycle>=2)return;
        if(FirstMeeting("guard","Petr Kříž • průvodce","guideInstruction",ShowGuideInstruction))return;
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
        guidePortraitSprite=Resources.Load<Sprite>("PetrKrizPortraitCutout");
        return guidePortraitSprite;
    }
}
