using UnityEngine;

public sealed partial class RpgGame
{
    public string DialogueNpcId { get; private set; }
    private string battleNpcId;
    public bool CanChallengeNpc(string id)
    {
        if(string.IsNullOrEmpty(id) || State.NpcDefeated(id) || InBattle || IsEnteringRoom || State.health<=0)return false;
        if(id=="merchant" && Interface.InRoom)return true;
        var target=worldItems.Find(item=>item.id==id && item.obj!=null && item.obj.activeSelf);
        return target!=null && Vector2.Distance(transform.position,target.obj.transform.position)<=3;
    }
    public void StartNpcBattle(string id)
    {
        if(!CanChallengeNpc(id))return;
        var target=worldItems.Find(item=>item.id==id && item.obj!=null);
        string title=target!=null?target.title:id=="merchant"?"Obchodnice Eliška":"Obyvatel";
        Interface.Close();Save(false);
        battleEnemy=null;battleNpcId=id;
        battle=new RpgTurnBattle(State,id=="guard"?72:48,false,title);
        Interface.ShowBattle(battle);
    }
    private void NpcVictory(string id)
    {
        if(!State.DefeatNpc(id))return;
        foreach(var target in worldItems.FindAll(item=>item.id==id))
        {
            if(target.obj==null)continue;
            worldObjects.Remove(target.obj);target.obj.SetActive(false);Destroy(target.obj);
        }
        worldItems.RemoveAll(item=>item.id==id);
        if(id=="healer")BuildCompanion();
        DialogueNpcId=null;
        Notify("NPC poraženo. "+(id=="guard" || id=="elder" || id=="merchant"?
            "Zůstaly po něm poznámky k cestě za svíčkami. Postup najdeš v mobilu [Q].":""),7);
    }
}

public sealed partial class RpgInterface
{
    private void NpcChallenge(string id)
    {
        if(game.CanChallengeNpc(id))Action("Vyzvat k souboji",()=>game.StartNpcBattle(id));
    }
    public void ShowFallenGuideNotes()
    {
        var s=game.State;
        Open("cellarJournal","PRŮVODCOVY POZNÁMKY",
            "Zůstaly po něm poznámky: Matěj ví o svíčkách ve sklepě obchodu. Pro rituál jsou potřeba tři. Ve zřícenině kostela je rozmísti do rohů trojúhelníku a zapal je. Podle zápisků tě to má probudit ze snu.\n\n"+s.CellarObjective);
        if(s.cellarQuest==6 && !s.ritualKnown)Action("Použít pokyny k rituálu",game.CompleteCellarQuest);
        Action("Zpět k úkolům",ShowPhoneJournal);
        Action("Odložit mobil",Close);
    }
    private void ShowEmptyShop()
    {
        if(game.State.cellarQuest>=2 && game.State.cellarQuest<6){ShowCellarMerchant();return;}
        Open("cellarClue","OPUŠTĚNÝ OBCHOD","Eliška je poražená. Nikdo tu neprodává ani nevydává její úkol. Zůstaly tu zásoby a přístup do sklepa.");
        Action("Odejít",Close);
    }
}
