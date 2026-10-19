using UnityEngine;

public sealed partial class RpgState
{
    public const int SkillCount=8;
    public int BowDamage => 16+(HasSkill(1)?6:0)+(HasSkill(7)?6:0);
    public float EscapeChance => HasSkill(6)?.75f:.5f;
    public float BlockChance => HasSkill(5)?.5f:.35f;
    public bool CanBuyBowFrom(string npc) => !NpcDefeated(npc) && (npc=="districtRybář Oldřich" || npc=="districtKovář Mikuláš" && NpcDefeated("districtRybář Oldřich"));
    public bool HunterStashAvailable => cycle>=3 && NpcDefeated("districtRybář Oldřich") && (!hasBow || !bowTrained);
    public bool RecoverHunterBow() => HunterStashAvailable && health>0 && BuyBow();
    public bool StudyHunterNotes() => HunterStashAvailable && LearnCombat(true,false);
    public bool ParcelRecipientMissing => deliveryQuest==1 && !parcelDelivered && NpcDefeated("guard");
    public bool CancelUndeliverableParcel()
    {
        if(!ParcelRecipientMissing)return false;
        deliveryQuest=0;if(trackedQuest==2)trackedQuest=-1;
        return true;
    }
    public void RecoverAfterDefeat()
    {
        deaths++;gold=Mathf.Max(0,gold-10);health=MaxHealth;stamina=MaxStamina;
        place=RpgPlace.Town;x=.5f;y=3.5f;
        // A failed defence must be attempted in full, rather than chipped away
        // through repeated full-health respawns.
        if(FinalWaveActive){finalRitualStarted=false;finalEnemiesRemaining=0;}
    }
}

public sealed partial class RpgGame
{
    private void BuildHunterStash()
    {
        if(IsTestLocation || !InTown || !State.HunterStashAvailable || worldItems.Exists(w=>w.id=="hunterStash"))return;
        var position=RpgWorldGeometry.ExpandedPixelPosition(new Vector2(1340,570),map.bounds);
        AddPickup("hunterStash","Oldřichova výbava a zápisky","hunterStash",position,Color.yellow);
    }
    public void UseHunterStash(bool buy)
    {
        var stash=worldItems.Find(w=>w.id=="hunterStash" && w.obj!=null);
        if(stash==null || Vector2.Distance(transform.position,stash.obj.transform.position)>3 || !State.NpcDefeated("districtRybář Oldřich"))return;
        if(buy)
        {
            if(!State.RecoverHunterBow()){Notify("Na luk potřebuješ 40 zlata.");return;}
            Notify("Luk získán. V truhle zůstalo 40 zlata pro Oldřichovu rodinu.");
        }
        else if(State.StudyHunterNotes())Notify("Oldřichovy zápisky tě naučily zacházet s lukem.");
        Save(false);Interface.ShowHunterStash();
    }
    public void ReturnUndeliverableParcel()
    {
        if(DialogueNpcId!="merchant" && !Interface.InRoom || State.NpcDefeated("merchant") || !State.CancelUndeliverableParcel())return;
        Save(false);Notify("Zásilka vrácena Elišce. Úkol zrušen bez odměny.");Interface.ShowMerchant();
    }
}

public sealed partial class RpgInterface
{
    public void ShowHunterStash()
    {
        var s=game.State;
        Open("cellarClue","OLDŘICHOVA POZŮSTALOST","U přístaviště zůstala Oldřichova truhla s náhradním lukem a zápisky o střelbě. Za luk nech 40 zlata pro jeho rodinu. Zápisky si můžeš přečíst zdarma.");
        if(!s.hasBow)Action("Vzít luk a nechat 40 zlata",()=>game.UseHunterStash(true));
        if(!s.bowTrained)Action("Naučit se střelbu ze zápisků",()=>game.UseHunterStash(false));
        Action("Odejít",Close);
    }
    private void ShowSkillPage(int page)
    {
        var s=game.State;if(s.health<=0)return;
        string[] names={"Vitalita","Silný úder","Úhyb","Rozhodující úder","Vytrvalec","Pevný kryt","Únikový plán","Ostré oko"};
        string[] descriptions={"+20 maximálního zdraví.","+6 poškození útoku.","15% šance vyhnout se zásahu.","10% šance okamžitě porazit běžného nepřítele.","+20 maximální výdrže.","Šance na úplný blok se zvýší z 35 % na 50 %.","Šance na útěk se zvýší z 50 % na 75 %.","+6 poškození při střelbě z luku."};
        string body=$"{s.HeroName} • cyklus {s.cycle} • volné body: {s.UpgradePoints}\nZa úkoly nejvýše 1 / 3 / 4 body podle cyklu. Každá dovednost stojí 1 bod. Body i dovednosti zůstávají napříč cykly.\n";
        for(int i=page*4;i<page*4+4;i++)body+="\n"+names[i]+": "+descriptions[i];
        Open("character","DOVEDNOSTI • "+(page+1)+" / 2",body);
        for(int i=page*4;i<page*4+4;i++)
        {
            int skill=i;Action(names[i]+(s.HasSkill(i)?" • odemčeno":" • 1 bod"),()=>{game.UpgradeCharacter(skill);ShowSkillPage(page);});
            actions.GetChild(actions.childCount-1).GetComponent<UnityEngine.UI.Button>().interactable=s.UpgradePoints>0&&!s.HasSkill(i);
        }
        Action(page==0?"Další dovednosti →":"← Předchozí dovednosti",()=>ShowSkillPage(1-page));
        Action("Zavřít",Close);
    }
}
