using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgGame
{
    public void StartCellarQuest()
    {
        if(State.cellarQuest != 0) return;
        State.cellarQuest=1; if(State.travelCompanion==1)State.travelCompanion=0;
        if(companionObject!=null) {worldObjects.Remove(companionObject);Destroy(companionObject);}
        BuildCompanion(); Interface.Close(); Save(false);
        Notify("Petr poradil: najdi dědu Matěje na náměstí [E].",7);
    }
    public void AdvanceCellarQuest(int stage)
    {
        State.cellarQuest=Mathf.Max(State.cellarQuest,stage); Save(false);
    }
    public void CompleteCellarQuest()
    {
        if(!State.FinishCellarQuest())return;
        Save(false); Interface.Close(); Notify("Tajemství sklepa: +45 zlata, +60 XP a 2 lektvary. Vodítko zůstává v deníku.",8);
    }
}

public sealed partial class RpgInterface
{
    private bool cellarAnimating;
    public bool StoryAnimating => cellarAnimating;
    private const string CellarClue="Až zazní třetí zvon, hledej pod uvolněným kamenem u východního mostu. Značka havrana ukazuje cestu ke starému úkrytu. Vezmi s sebou světlo.";

    public void ShowResident(string title)
    {
        Open("resident",title.ToUpperInvariant(),"Dobrý den, pocestný. Dnes mám plné ruce práce. Jestli něco sháníš, zeptej se dědy Matěje na náměstí nebo Elišky v obchodě. A Petr Kříž ti ukáže cestu.");
        Action("Poděkovat a odejít",Close);
    }
    public void ShowCellarGuide()
    {
        var s=game.State;
        Open("cellarGuide","PETR KŘÍŽ • PRŮVODCE",s.cellarQuest==0?
            "Jsem Petr Kříž. Občas se ti ozvu s radou, až budeš potřebovat. Na cestu potřebujeme svíčky. Zeptej se někoho místního — děda Matěj na náměstí ví snad o všem.":
            s.cellarQuest==6?"Tohle není obyčejný nákupní seznam… Havran a třetí zvon. Dobře, že jsi lístek otevřel. Schovej ho; u mostu nám ukáže další cestu. Tady máš zásoby za pomoc.":
            "Tady je moje rada pro další krok: "+s.CellarObjective);
        if(s.cellarQuest==0)Action("Rozumím • začít úkol",game.StartCellarQuest);
        if(s.cellarQuest==6)Action("Ukázat vzkaz a dokončit úkol",game.CompleteCellarQuest);
        if(s.deliveryQuest==1&&!s.parcelDelivered)Action("Předat zásilku od Elišky",game.DeliverParcel);
        Action("Zatím odejít",Close);
    }
    public void ShowCellarElder()
    {
        bool active=game.State.cellarQuest>0;
        Open("cellarElder","DĚDA MATĚJ",active?
            "Svíčky? Eliška je skladuje dole ve sklepě pod obchodem. Jen se jí nejdřív zeptej! Poslední dobou tam prý něco šramotí. Já už po těch schodech nelezu.":
            "Zdravím tě, pocestný. Hledáš někoho? Petr Kříž u východního mostu tě rád provede vesnicí.");
        if(game.State.cellarQuest==1)Action("Poděkovat a zeptat se Elišky",()=>{game.AdvanceCellarQuest(2);Close();});
        Action("Odejít",Close);
    }
    public void ShowCellarMerchant()
    {
        if(game.State.cellarQuest<2 || game.State.cellarQuest>=6){ShowMerchant();return;}
        game.AdvanceCellarQuest(3);
        Open("cellarMerchant","OBCHODNICE ELIŠKA","Svíčky ve sklepě? Nevím, kdo ti to řekl. Dolů tě nepustím… nic zajímavého tam není. Jen krysy! Velké krysy. Opravdu tam chceš lézt?");
        Action("Sejít opatrně do sklepa",()=>StartCoroutine(CellarEntry()));
        Action("Odejít z obchodu",Close);
        Action("Utéct zpátky na ulici",()=>{Close();game.Notify("Raději zpátky na čerstvý vzduch. Do sklepa se můžeš vrátit.");});
    }
    public void ShowCellarJournal()
    {
        var s=game.State;
        Open("cellarJournal","TAJEMSTVÍ SKLEPA",s.CellarObjective+"\n\nPrůvodce: Petr Kříž.\n"+
            (s.cellarNoteOpened?"Nalezený vzkaz:\n„"+CellarClue+"“":s.cellarNoteTaken?"V batohu máš složený lístek. Zatím jsi ho nečetl.":"Lístek zatím nemáš.")+
            "\n\nOdměna za ukázání vzkazu Petrovi: 45 zlata, 60 XP a 2 lektvary.");
        if(s.cellarNoteTaken&&!s.cellarNoteOpened)Action("Otevřít nalezený lístek",ReadCellarNote);
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
        LeaveRoomVisuals();
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
        for(int i=0;i<3;i++)CellarPiece("Svíčky",new Vector2(370+i*18,180),new Vector2(10,30),new Color(.86f,.76f,.48f));
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
    private IEnumerator CellarEntry()
    {
        cellarAnimating=true;
        var actor=BuildCellarScene();
        Open("cellarWalk","SCHODY DO SKLEPA","Vzpomeneš si na Petrovu radu: pomalu a opatrně…");
        yield return CellarWalk(actor,new Vector2(-110,80),2.5f);
        for(int i=0;i<5;i++)
        {
            var rat=CellarPiece("Krysa",new Vector2(50+i*65,70+(i%2)*70),new Vector2(32,20),new Color(.40f,.33f,.28f));
            rat.gameObject.AddComponent<CellarRatMotion>();
            var tail=Rect(rat,"Ocas",new Vector2(-15,-8),new Vector2(22,4));tail.gameObject.AddComponent<Image>().color=new Color(.6f,.4f,.35f);
            var eye=Rect(rat,"Oko",new Vector2(22,-3),new Vector2(4,4));eye.gameObject.AddComponent<Image>().color=Color.black;
        }
        game.AdvanceCellarQuest(4);cellarAnimating=false;
        Open("cellarRats",game.State.heroIsBoy?"HLAVNÍ HRDINA":"ARABELLA","Áá! Ona nelhala — tady opravdu jsou krysy!\n\nVzpomeneš si na radu: zpátky ke schodům, pomalu!");
        Action("Utéct ke schodům",()=>StartCoroutine(CellarEscape(actor)));
        Action("Opatrně ustoupit",()=>StartCoroutine(CellarEscape(actor)));
    }
    private IEnumerator CellarEscape(RectTransform actor)
    {
        cellarAnimating=true;
        Open("cellarEscape","ZPÁTKY KE SCHODŮM","Krysy zašustí mezi bednami. U schodu se ve světle zaleskne složený papírek…");
        yield return CellarWalk(actor,new Vector2(-400,215),1.6f);
        CellarPiece("Složený lístek",new Vector2(-350,210),new Vector2(28,22),new Color(.92f,.83f,.62f));
        game.AdvanceCellarQuest(5);cellarAnimating=false;
        Open("cellarNote","LÍSTEK NA SCHODU","Ve spěchu si všimneš papírku se znakem havrana. Možná je důležitý. Co uděláš?");
        if(!game.State.cellarNoteTaken)Action("Vzít a otevřít lístek",()=>{game.State.cellarNoteTaken=true;ReadCellarNote();});
        else Action("Otevřít lístek z batohu",ReadCellarNote);
        Action("Jít dál • nechat lístek být",()=>{Close();game.Notify("Vyšel jsi ze sklepa. Lístek můžeš později vyzvednout při návratu.",6);});
    }
    private void ReadCellarNote()
    {
        game.State.OpenCellarNote();game.Save(false);
        Open("cellarClue","VZKAZ SE ZNAKEM HAVRANA","„"+CellarClue+"“\n\nTenhle vzkaz se může hodit. Venku se na něj zeptej Petra.\nVzkaz byl uložen do deníku [Q].");
        Action("Schovat lístek a odejít",Close);
    }
}

public sealed class CellarRatMotion : MonoBehaviour
{
    private Vector2 origin;
    private RectTransform rect;
    private void Start(){rect=GetComponent<RectTransform>();origin=rect.anchoredPosition;}
    private void Update(){if(rect!=null)rect.anchoredPosition=origin+new Vector2(Mathf.Sin(Time.unscaledTime*3+origin.x)*18,Mathf.Cos(Time.unscaledTime*2+origin.y)*9);}
}
