using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public static class RpgTurnCombatValidation
{
    public static int CheckRules()
    {
        int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;};
        var s=RpgState.CreateNew(false);s.cycle=3;
        check(s.UpgradePoints==0 && s.unlockedSkills.Count==0,"Fresh story has no points or unlocked skills");
        check(!s.UpgradeAttribute(0),"Unlocking requires a quest point");
        check(s.RewardQuest("a") && !s.RewardQuest("a") && s.UpgradePoints==1,"Quest rewards are once per quest and cycle");
        check(s.UpgradeAttribute(0) && s.MaxHealth==120 && s.health==120 && s.UpgradePoints==0,"Vitality is unlocked once for one point");
        s.RewardQuest("b");check(!s.UpgradeAttribute(0) && s.UpgradePoints==1,"Unlocked skills cannot consume further points");
        check(s.UpgradeAttribute(1) && s.Damage==14,"Attack skill adds six damage");
        s.RewardQuest("c");s.RewardQuest("d");s.UpgradeAttribute(2);s.UpgradeAttribute(3);
        check(s.DodgeChance==.15f && s.InstantWinChance==.10f,"Chance skills expose their fixed bonuses");
        check(!s.UpgradeAttribute(-1) && !s.UpgradeAttribute(4),"Invalid skills are rejected");
        check(!s.AddExperience(100000) && s.level==1 && s.experience==0 && s.UpgradePoints==0,"XP does not award levels or points");
        var containers=RpgState.CreateNew(false);
        check(!containers.QuestContainersVisible,"Containers are hidden before the search quest");
        containers.AcceptSideQuest(1);check(containers.QuestContainersVisible,"Accepting the search quest reveals containers");
        containers.collected.Add("supply1");containers.collected.Add("supply2");containers.FinishSideQuest(1);
        check(!containers.QuestContainersVisible,"Turning in the search quest hides remaining containers");
        var capped=RpgState.CreateNew(false);capped.health=98;
        check(capped.DrinkPotion() && capped.health==100 && capped.potions==2,"Fifteen-point potion respects maximum health");
        var battle=new RpgTurnBattle(ReadyFighter(),48,false);
        check(battle.Round==1 && battle.EnemyHealth==48 && battle.Player.health==100,"Waiting does not advance the fight");
        check(battle.Act(RpgBattleAction.Attack,1,1,1) && battle.EnemyHealth==30 && battle.Player.health==91 && battle.Round==2,"Attack gets exactly one enemy response");
        battle.Act(RpgBattleAction.Guard,1,1,1);
        check(battle.Player.health==86 && battle.Round==3,"Guard halves the next hit");
        battle=new RpgTurnBattle(ReadyFighter(),48,false);
        check(!battle.Act(RpgBattleAction.Potion,1,1,1) && battle.Round==1 && battle.Player.health==100 && battle.Player.potions==3,"Invalid potion does not consume a turn or item");
        battle.Player.health=20;battle.Act(RpgBattleAction.Potion,1,1,1);
        check(battle.Player.health==26 && battle.Player.potions==2 && battle.Round==2,"Potion heals before the enemy's response");
        battle=new RpgTurnBattle(ReadyFighter(),8,false);battle.Act(RpgBattleAction.Attack,1,1,1);
        check(battle.Result==RpgBattleResult.Victory && battle.Player.health==100,"Defeated enemies cannot counterattack");
        check(!battle.Act(RpgBattleAction.Attack,1,1,1),"Finished fights reject more actions");
        var agile=ReadyFighter();agile.unlockedSkills.Add(2);
        battle=new RpgTurnBattle(agile,48,false);battle.Act(RpgBattleAction.Attack,1,.1f,1);
        check(agile.health==100,"Unlocked dodge can avoid an enemy hit");
        var lucky=ReadyFighter();lucky.unlockedSkills.Add(3);
        battle=new RpgTurnBattle(lucky,48,false);battle.Act(RpgBattleAction.Attack,.05f,1,1);
        check(battle.Result==RpgBattleResult.Victory && lucky.health==100,"Instant strike defeats regular enemies before their turn");
        battle=new RpgTurnBattle(lucky,220,true);battle.Act(RpgBattleAction.Attack,.05f,1,1);
        check(battle.Result==RpgBattleResult.Ongoing && battle.EnemyHealth==202 && lucky.health==78,"Bosses resist instant defeat");
        battle=new RpgTurnBattle(ReadyFighter(),48,false);battle.Act(RpgBattleAction.Escape,1,1,.2f);
        check(battle.Result==RpgBattleResult.Escaped && battle.Player.health==100,"Successful escape has no enemy response");
        battle=new RpgTurnBattle(ReadyFighter(),48,false);battle.Act(RpgBattleAction.Escape,1,1,.8f);
        check(battle.Result==RpgBattleResult.Ongoing && battle.Player.health==91 && battle.Round==2,"Failed escape allows one enemy response");
        battle.Player.health=1;battle.Act(RpgBattleAction.Guard,1,1,1);
        check(battle.Result==RpgBattleResult.Defeat && battle.Player.health==0,"Enemy turns can defeat the player");
        var quest=RpgState.CreateNew(false);quest.cycle=2;quest.suppliesQuest=1;quest.collected.Add("supply1");quest.collected.Add("supply2");
        check(quest.FinishSideQuest(1) && quest.UpgradePoints==1 && !quest.FinishSideQuest(1),"Side quest awards one point once");
        quest.questStage=1;quest.banditsDefeated=5;quest.bossDefeated=true;
        check(quest.FinishQuest() && quest.UpgradePoints==2 && !quest.FinishQuest(),"Main quest awards one point once");
        var ritual=RpgState.CreateNew(false);ritual.cellarQuest=4;ritual.CollectCandles(false);ritual.ExplainRitual();
        check(ritual.UpgradePoints==1 && !ritual.ExplainRitual(),"Returning the candles grants one quest point");
        ritual.UpgradeAttribute(0);
        ritual.upgradePoints=3;for(int id=1;id<4;id++)ritual.UpgradeAttribute(id);
        ritual.upgradePoints=1;ritual.place=RpgPlace.ChurchRuins;var next=ritual.BeginSecondCycle();
        check(next.HasSkill(0) && next.health==120 && next.MaxHealth==120 && next.UpgradePoints==1 && next.rewardedQuests.Count==0,"Cycle preserves vitality and unused points but resets quest history");
        check(next.HasSkill(1) && next.HasSkill(2) && next.HasSkill(3) && next.Damage==14 && next.DodgeChance==.15f && next.InstantWinChance==.1f,"All four unlocked skills keep their effects across cycles");
        check(!ReferenceEquals(next.unlockedSkills,ritual.unlockedSkills) && ritual.UpgradePoints==1,"Cycle copies skills without sharing state and preserves unused points");
        next.Normalize();check(next.unlockedSkills.Count==4 && next.health==120,"Normalization preserves permanent skills");
        foreach(var field in typeof(RpgState).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))
        {
            if(field.Name=="cycle" || field.Name=="ritualFailed" || field.Name=="deaths" || field.Name=="unlockedSkills" || field.Name=="health" || field.Name=="upgradePoints")continue;
            var fresh=RpgState.CreateNew(next.heroIsBoy,next.HeroName);
            object value=field.GetValue(next),expected=field.GetValue(fresh);
            if(value is System.Collections.IList list)check(list.Count==0,"Cycle resets collection: "+field.Name);
            else check(Equals(value,expected),"Cycle resets field: "+field.Name);
        }
        check(RpgState.CreateNew(next.heroIsBoy,next.HeroName).unlockedSkills.Count==0,"Starting a new story clears permanent skills");
        var legacy=new RpgState{level=8,experience=99,vitalityRank=2,suppliesQuest=2};legacy.Normalize();
        check(legacy.level==1 && legacy.experience==0 && legacy.HasSkill(0) && legacy.UpgradePoints==1,"Legacy saves migrate completed quests and old upgrades");
        legacy.Normalize();check(legacy.UpgradePoints==1,"Migration cannot duplicate quest points");
        var equipment=RpgState.CreateNew(false);
        check(!equipment.hasIronSword && equipment.Damage==8,"New game starts unarmed");
        equipment.gold=100;check(!equipment.BuyIronSword() && equipment.gold==100,"Sword cannot be bought in cycle one");
        equipment.cycle=2;equipment.gold=39;check(!equipment.BuyIronSword() && equipment.gold==39,"Sword needs full price");
        equipment.gold=100;check(equipment.BuyIronSword() && equipment.gold==60 && equipment.hasIronSword && equipment.Damage==18,"Cycle two sword purchase equips sword and charges once");
        check(!equipment.BuyIronSword() && equipment.gold==60,"Owned sword cannot be bought twice");
        equipment.Normalize();check(equipment.hasIronSword && equipment.archiveItems.Contains("sword"),"Bought sword survives normalization");
        equipment.cycle=1;equipment.herbs=4;equipment.herbsQuest=2;equipment.knownSideQuests.Add(0);equipment.archiveItems.Add("herb");equipment.trackedQuest=0;equipment.Normalize();
        check(!equipment.hasIronSword && equipment.weaponLevel==0 && !equipment.archiveItems.Contains("sword"),"Legacy first cycle swords are removed");
        check(equipment.herbs==0 && equipment.herbsQuest==0 && !equipment.KnowsSideQuest(0) && !equipment.AcceptSideQuest(0) && !equipment.FinishSideQuest(0) && equipment.trackedQuest==-1 && !equipment.archiveItems.Contains("herb"),"Removed herbs and quest cannot return from legacy saves");
        check(!RpgState.CreateNew(false).hasIronSword,"New story clears sword ownership");
        return count+CheckNpcRules();
    }
    private static RpgState ReadyFighter(){var s=RpgState.CreateNew(false);s.cycle=2;s.hasIronSword=true;s.swordTrained=true;return s;}
    public static int CheckNpcRules()
    {
        int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;};
        var s=RpgState.CreateNew(false);
        check(!s.NpcDefeated("mayor") && s.defeatedNpcs.Count==0,"Fresh game has all residents alive");
        check(s.DefeatNpc("mayor") && !s.DefeatNpc("mayor") && s.defeatedNpcs.Count==1,"NPC victories are recorded once");
        check(s.UpgradePoints==0 && s.gold==35 && s.banditsDefeated==0 && !s.bossDefeated,"NPC victories grant no quest points or hostile campaign progress");
        check(!s.DefeatNpc("guard") && !s.CanDefeatNpc("guard") && s.cellarQuest==0,"Petr is protected before his quest");
        s.cellarQuest=1;
        check(s.DefeatNpc("elder") && s.cellarQuest==2,"Defeated elder leaves the shop clue");
        check(s.DefeatNpc("merchant") && s.cellarQuest==3 && s.CellarObjective.Contains("opuštěný"),"Defeated merchant leaves a usable cellar route");
        s.cellarQuest=4;check(s.CollectCandles(false) && s.candles==3 && !s.CanDefeatNpc("guard"),"Collecting the candles does not yet unlock fighting Petr");
        check(s.ExplainRitual() && s.UpgradePoints==1 && !s.ExplainRitual(),"Guide notes unlock the ritual and award its quest point once");
        check(s.CanDefeatNpc("guard") && s.PetrQuestComplete,"Completing Petr's candle quest unlocks his battle");
        check(s.DefeatNpc("guard") && s.candles==4 && s.petrCandleTaken,"Petr gives exactly one extra candle on defeat");
        check(!s.DefeatNpc("guard") && s.candles==4,"Petr's candle cannot be collected twice");
        s.Normalize();check(s.candles==4 && s.NpcDefeated("guard") && s.petrCandleTaken,"Loading preserves the fourth candle and Petr's defeat");
        s.place=RpgPlace.ChurchRuins;check(s.CanPerformRitual,"Four candles still allow the three-candle ritual");
        var next=s.BeginSecondCycle();
        check(next.cycle==2 && next.defeatedNpcs.Count==0 && !next.NpcDefeated("guard"),"NPCs return in the next cycle");
        check(next.UpgradePoints==1 && s.defeatedNpcs.Count==4 && next.candles==0 && !next.petrCandleTaken,"New cycle discards the bonus candle, keeps points and resets Petr reward");
        var early=RpgState.CreateNew(false);early.DefeatNpc("elder");early.DefeatNpc("merchant");
        early.cellarQuest=1;early.ResolveNpcClues();check(early.cellarQuest==3,"Clues work when NPCs were defeated before accepting the guide quest");
        early.defeatedNpcs.Add("elder");early.defeatedNpcs.Add(null);early.Normalize();
        check(early.defeatedNpcs.Count==2,"Loading removes duplicate and empty NPC records");
        var oldProtected=RpgState.CreateNew(false);oldProtected.cellarQuest=1;oldProtected.defeatedNpcs.Add("guard");oldProtected.Normalize();
        check(!oldProtected.NpcDefeated("guard") && !oldProtected.CanDefeatNpc("guard"),"Old saves restore Petr when his quest is unfinished");
        var oldVictory=new RpgState{cellarQuest=7,ritualKnown=true,candles=3};oldVictory.defeatedNpcs.Add("guard");oldVictory.Normalize();oldVictory.Normalize();
        check(oldVictory.candles==4 && oldVictory.petrCandleTaken,"Older completed-quest victories get one bonus candle without duplicating it");
        var named=new RpgTurnBattle(ReadyFighter(),72,false,"Petr");
        named.Act(RpgBattleAction.Escape,1,1,0);
        check(named.OpponentName=="Petr" && named.EnemyHealth==72 && named.Result==RpgBattleResult.Escaped,"Named NPC opponents use the turn system and can be escaped");
        check(!named.Player.NpcDefeated("guard"),"Escaping does not defeat or remove the NPC");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Turn Combat and Skills")]
    public static void Run()
    {
        int count=CheckRules();
        var state=RpgState.CreateNew(false);state.cycle=2;state.RewardQuest("a");state.UpgradeAttribute(3);state.RewardQuest("b");
        var saved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));saved.Normalize();
        if(saved.UpgradePoints!=1 || !saved.HasSkill(3) || saved.RewardQuest("a"))throw new Exception("Skill save round trip");
        state.DefeatNpc("healer");
        var npcSaved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));npcSaved.Normalize();
        if(!npcSaved.NpcDefeated("healer") || npcSaved.NpcDefeated("guard"))throw new Exception("NPC save round trip");
        state.cellarQuest=7;state.ritualKnown=true;state.candles=3;state.DefeatNpc("guard");
        var petrSaved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));petrSaved.Normalize();
        if(petrSaved.candles!=4 || !petrSaved.petrCandleTaken || !petrSaved.NpcDefeated("guard") || petrSaved.DefeatNpc("guard"))throw new Exception("Petr bonus candle save round trip");
        Debug.Log("PASS: "+(count+3)+" turn combat, skill, reward and save checks.");
    }
#endif
}
