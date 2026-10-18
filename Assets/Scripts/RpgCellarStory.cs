using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgGame
{
    public void StartCellarQuest()
    {
        if(State.cellarQuest != 0) return;
        State.cellarQuest=1; State.ResolveNpcClues(); if(State.travelCompanion==1)State.travelCompanion=0;
        if(companionObject!=null) {worldObjects.Remove(companionObject);Destroy(companionObject);}
        BuildCompanion(); Interface.Close(); Save(false);
        Notify(State.CellarObjective,7);
    }
    public void AdvanceCellarQuest(int stage)
    {
        State.cellarQuest=Mathf.Max(State.cellarQuest,stage); State.ResolveNpcClues(); Save(false);
    }
    public void CompleteCellarQuest()
    {
        int points=State.UpgradePoints;
        if(!State.ExplainRitual())return;
        Save(false); Interface.Close(); Notify((State.NpcDefeated("guard")?"Pokyny z poznámek přečteny":"Svíčky doručeny")+(State.UpgradePoints>points?" • +1 bod vylepšení.":" • Bodový limit cyklu již dosažen.")+" Ve zřícenině kostela proveď rituál [E].",8);
    }
}

public sealed partial class RpgInterface
{
    private bool cellarAnimating,inCellar;
    public bool StoryAnimating => cellarAnimating;
    public void ShowResident(string title)
    {
        if(FirstMeeting(game.DialogueNpcId,title,"resident",()=>ShowResident(title)))return;
        if(game.DialogueNpcId=="districtKovář Mikuláš"){ShowBlacksmith();return;}
        if(game.DialogueNpcId=="districtRybář Oldřich"){ShowExistingHunter();return;}
        Open("resident",title.ToUpperInvariant(),"Hledáš další cestu? "+game.State.CellarObjective);
        NpcChallenge(game.DialogueNpcId);
        Action("Odejít",Close);
    }
    private void ShowBlacksmith()
    {
        var state=game.State;
        Open("resident","KOVÁŘ MIKULÁŠ",state.cycle<2?
            "Dnes nemám žádný železný meč na prodej. Zkus se vrátit později.":
            $"Zlato: {state.gold}\n\n"+(state.hasIronSword?"Mohu ti vylepšit železný meč.":"Železný meč je na prodej za 40 zlata."));
        if(state.cycle>=2 && !state.hasIronSword)Action("Koupit železný meč • 40 zl.",()=>{game.BuyIronSword();ShowBlacksmith();});
        if(state.cycle>=2 && state.hasIronSword && state.weaponLevel<5)Action($"Vylepšit meč • {40+state.weaponLevel*30} zl.",()=>{game.Upgrade(true);ShowBlacksmith();});
        if(state.cycle>=2 && !state.swordTrained)Action("Výcvik s mečem • 30 zl.",()=>{game.LearnCombat(false,true);ShowBlacksmith();});
        NpcChallenge(game.DialogueNpcId);Action("Odejít",Close);
    }
    public void ShowCellarGuide()
    {
        if(game.State.NpcDefeated("guard")){ShowFallenGuideNotes();return;}
        if(FirstMeeting("guard","Petr Kříž • průvodce","cellarGuide",ShowCellarGuide))return;
        var s=game.State;
        Open("cellarGuide","PETR KŘÍŽ • PRŮVODCE",
            (s.cycle>1?"Nepamatuji si, že bychom se dříve setkali. ":"")+s.CellarObjective+
            $"\n\nRituál: {s.RitualShape}, {s.RequiredCandles} svíček. Tři získáš ve sklepě."+
            (s.cycle>1?" Další hledej v lese nebo za regálem knihovny.":""));
        if(s.cellarQuest==0)Action("Zeptat se na svíčky",game.StartCellarQuest);
        if(s.cellarQuest==6)Action("Mám svíčky • přijmout radu",game.CompleteCellarQuest);
        if(s.cycle>=2 && !s.swordTrained)Action("Naučit se boj s mečem • zdarma",()=>{game.LearnCombat(false,false);ShowCellarGuide();});
        if(s.cycle>=3 && !s.bowTrained)Action("Naučit se boj s lukem • zdarma",()=>{game.LearnCombat(true,false);ShowCellarGuide();});
        NpcChallenge("guard");
        if(s.deliveryQuest==1&&!s.parcelDelivered)Action("Předat zásilku od Elišky",game.DeliverParcel);
        Action("Odejít",Close);
    }
    public void ShowCellarElder()
    {
        if(FirstMeeting("elder","Děda Matěj","cellarElder",ShowCellarElder))return;
        bool active=game.State.cellarQuest>0;
        Open("cellarElder","DĚDA MATĚJ",active?
            "Svíčky? Eliška je skladuje dole ve sklepě pod obchodem. Jen se jí nejdřív zeptej! Poslední dobou tam prý něco šramotí. Já už po těch schodech nelezu.":
            "Zdravím tě, pocestný. Hledáš někoho? Petr Kříž u východního mostu tě rád provede vesnicí.");
        if(game.State.cellarQuest==1)Action("Poděkovat a zeptat se Elišky",()=>{game.AdvanceCellarQuest(2);Close();});
        NpcChallenge("elder");
        Action("Odejít",Close);
    }
    public void ShowCellarMerchant()
    {
        if(FirstMeeting("merchant","Obchodnice Eliška","cellarMerchant",ShowCellarMerchant))return;
        if(game.State.cellarQuest<2 || game.State.cellarQuest>=6){ShowMerchant();return;}
        game.AdvanceCellarQuest(3);
        bool absent=game.State.NpcDefeated("merchant");
        Open(absent?"cellarClue":"cellarMerchant",absent?"OPUŠTĚNÝ OBCHOD":"OBCHODNICE ELIŠKA",absent?"V Eliščiných poznámkách je zmínka o svíčkách ve sklepě. Schody jsou přístupné, ale zdola se ozývá šramot.":"Svíčky ve sklepě? Nevím, kdo ti to řekl. Dolů tě nepustím… nic zajímavého tam není. Jen krysy! Velké krysy. Opravdu tam chceš lézt?");
        NpcChallenge("merchant");
        Action("Vstup do sklepa",ShowCellarEntrance);
        Action("Odejít z obchodu",Close);
        Action("Utéct zpátky na ulici",()=>{Close();game.Notify("Raději zpátky na čerstvý vzduch. Do sklepa se můžeš vrátit.");});
    }
    public void ShowCellarJournal()
    {
        var s=game.State;
        Open("cellarJournal","SVÍČKY A RITUÁL • CYKLUS "+s.cycle,s.CellarObjective+"\n\nSvíčky: "+s.candles+" • rituál vyžaduje "+s.RequiredCandles+
            (s.ritualFailed?"\nRituál skončil smrtí. Pamatuješ si první cyklus.":""));
        Action("Zavřít deník",Close);
    }
    private RectTransform CellarPiece(string name,Vector2 position,Vector2 size,Color color)
    {
        var rect=Rect(roomView,name,position,size);
        rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
        return rect;
    }
    private RectTransform BuildCellarScene()
    {
        if(!game.IsTestLocation){game.State.RecordArchivePlace("cellar");game.Save(false);}
        LeaveRoomVisuals();inCellar=true;
        roomView=Rect(canvas,"Sklep • pohled shora",Vector2.zero,Vector2.zero);
        roomView.anchorMin=Vector2.zero;roomView.anchorMax=Vector2.one;roomView.offsetMin=roomView.offsetMax=Vector2.zero;
        roomView.SetSiblingIndex(modal.GetSiblingIndex());
        roomView.gameObject.AddComponent<Image>().color=new Color(.045f,.038f,.03f);
        for(int y=0;y<12;y++)for(int x=0;x<20;x++)
            CellarPiece("Kamenná podlaha",new Vector2((x-9.5f)*64,(y-5.5f)*48),new Vector2(62,46),new Color(.17f+(x+y)%3*.012f,.145f,.115f));
        for(int i=0;i<6;i++)CellarPiece("Schod",new Vector2(-480,285-i*16),new Vector2(110,14),new Color(.38f-i*.025f,.32f-i*.02f,.24f));
        for(int i=0;i<4;i++)
        {
            CellarPiece("Bedna",new Vector2(370+i%2*90,150-i/2*120),new Vector2(78,78),new Color(.34f,.21f,.10f));
            CellarPiece("Prkno",new Vector2(370+i%2*90,150-i/2*120),new Vector2(6,78),new Color(.18f,.10f,.05f));
        }
        for(int i=0;i<RpgState.FirstCycleCandles;i++)CellarPiece("Svíčky",new Vector2(370+i*18,180),new Vector2(10,30),new Color(.86f,.76f,.48f));
        var hero=CellarPiece("Hlavní postava",new Vector2(-480,280),new Vector2(76,94),Color.white);
        hero.GetComponent<Image>().sprite=game.HeroSprite;hero.GetComponent<Image>().preserveAspect=true;

        return hero;
    }
    private IEnumerator CellarWalk(RectTransform actor,Vector2 target,float duration)
    {
        Vector2 start=actor.anchoredPosition;float t=0;
        var art=new HeroWalkAnimation(game.HeroSprite);
        while(t<duration && actor!=null)
        {
            t+=Time.unscaledDeltaTime;actor.anchoredPosition=Vector2.Lerp(start,target,t/duration);
            actor.GetComponent<Image>().sprite=art.Frame(0,((int)(t*7)%4)==0?1:((int)(t*7)%4)==2?2:0,game.State.heroIsBoy);
            yield return null;
        }
        if(actor!=null)actor.GetComponent<Image>().sprite=game.HeroSprite;
        art.Dispose();
    }
    private void ShowCellarEntrance()
    {
        ShowEntrance("Sklep obchodu",()=>StartCoroutine(CellarEntry()),ShowCellarMerchant);
    }
    private void ReturnFromCellar()
    {
        if(cellarAnimating)return;
        StartCoroutine(CellarReturn());
    }
    private IEnumerator CellarReturn()
    {
        cellarAnimating=true;
        yield return FadeLocation(0,1);
        EnterRoom("merchant");
        yield return FadeLocation(1,0);
        cellarAnimating=false;
    }
    private IEnumerator CellarEntry()
    {
        cellarAnimating=true;
        yield return FadeLocation(0,1);
        var actor=BuildCellarScene();
        Open("cellarWalk","SCHODY DO SKLEPA","Vzpomeneš si na Petrovu radu: pomalu a opatrně…");
        yield return FadeLocation(1,0);
        yield return CellarWalk(actor,new Vector2(-110,80),2.5f);
        for(int i=0;i<5;i++)
        {
            var rat=CellarPiece("Krysa",new Vector2(50+i*65,70+(i%2)*70),new Vector2(32,20),new Color(.40f,.33f,.28f));
            rat.gameObject.AddComponent<CellarRatMotion>();
            var tail=Rect(rat,"Ocas",new Vector2(-15,-8),new Vector2(22,4));tail.gameObject.AddComponent<Image>().color=new Color(.6f,.4f,.35f);
            var eye=Rect(rat,"Oko",new Vector2(22,-3),new Vector2(4,4));eye.gameObject.AddComponent<Image>().color=Color.black;
        }
        game.AdvanceCellarQuest(4);cellarAnimating=false;
        Open("cellarRats",game.State.HeroName,"Mezi tebou a svíčkami pobíhají krysy. Můžeš utéct, rychle pro svíčky skočit, nebo k nim opatrně dojít.");
        Action("Utéct",()=>StartCoroutine(CellarFlee(actor)));
        Action("Rychle skočit pro svíčky",()=>StartCoroutine(CellarCollect(actor,true)));
        Action("Opatrně dojít pro svíčky",()=>StartCoroutine(CellarCollect(actor,false)));
    }
    private IEnumerator CellarFlee(RectTransform actor)
    {
        if(cellarAnimating)yield break;
        cellarAnimating=true;
        Open("cellarEscape","ÚTĚK","Vracíš se ke schodům. Svíčky zůstávají ve sklepě.");
        yield return CellarWalk(actor,new Vector2(-480,280),1);
        cellarAnimating=false;Close();game.Notify("Ze sklepa jsi utekl bez svíček. Můžeš se vrátit.",6);
    }
    private IEnumerator CellarCollect(RectTransform actor,bool rush)
    {
        if(cellarAnimating || game.State.cellarCandlesTaken)yield break;
        cellarAnimating=true;
        Open("cellarWalk","CESTA KE SVÍČKÁM",rush?"Vrhnul ses mezi krysy. Jedna tě kousla, ale svíčky máš na dosah.":"Krok za krokem obcházíš krysy. Ke svíčkám se dostáváš bez zranění.");
        yield return CellarWalk(actor,new Vector2(360,185),rush?.65f:3f);
        if(game.State.CollectCandles(rush))
        {
            foreach(var image in roomView.GetComponentsInChildren<Image>())
                if(image.name=="Svíčky")image.gameObject.SetActive(false);
            game.Save(false);
        }
        cellarAnimating=false;
        Open("cellarClue","SVÍČKY ZÍSKÁNY",rush?"Máš všechny tři svíčky. Kousnutí tě stálo až 20 HP. Vrať se k průvodci a řekni mu, že je máš.":"Máš všechny tři svíčky a vyvázl jsi bez zranění. Vrať se k průvodci a řekni mu, že je máš.");
        Action("Vrátit se do obchodu",Close);
    }
}

public sealed class CellarRatMotion : MonoBehaviour
{
    private Vector2 origin;
    private RectTransform rect;
    private void Start(){rect=GetComponent<RectTransform>();origin=rect.anchoredPosition;}
    private void Update(){if(rect!=null)rect.anchoredPosition=origin+new Vector2(Mathf.Sin(Time.unscaledTime*3+origin.x)*18,Mathf.Cos(Time.unscaledTime*2+origin.y)*9);}
}
