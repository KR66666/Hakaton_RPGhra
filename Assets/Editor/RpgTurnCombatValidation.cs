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
        var s=RpgState.CreateNew(false);
        check(s.UpgradePoints==0 && s.unlockedSkills.Count==0,"Fresh story has no points or unlocked skills");
        check(!s.UpgradeAttribute(0),"Unlocking requires a quest point");
        check(s.RewardQuest("a") && !s.RewardQuest("a") && s.UpgradePoints==1,"Quest rewards are once per quest and cycle");
        check(s.UpgradeAttribute(0) && s.MaxHealth==120 && s.health==120 && s.UpgradePoints==0,"Vitality is unlocked once for one point");
        s.RewardQuest("b");check(!s.UpgradeAttribute(0) && s.UpgradePoints==1,"Unlocked skills cannot consume further points");
        check(s.UpgradeAttribute(1) && s.Damage==24,"Attack skill adds six damage");
        s.RewardQuest("c");s.RewardQuest("d");s.UpgradeAttribute(2);s.UpgradeAttribute(3);
        check(s.DodgeChance==.15f && s.InstantWinChance==.10f,"Chance skills expose their fixed bonuses");
        check(!s.UpgradeAttribute(-1) && !s.UpgradeAttribute(4),"Invalid skills are rejected");
        check(!s.AddExperience(100000) && s.level==1 && s.experience==0 && s.UpgradePoints==0,"XP does not award levels or points");
        var battle=new RpgTurnBattle(RpgState.CreateNew(false),48,false);
        check(battle.Round==1 && battle.EnemyHealth==48 && battle.Player.health==100,"Waiting does not advance the fight");
        check(battle.Act(RpgBattleAction.Attack,1,1,1) && battle.EnemyHealth==30 && battle.Player.health==91 && battle.Round==2,"Attack gets exactly one enemy response");
        battle.Act(RpgBattleAction.Guard,1,1,1);
        check(battle.Player.health==86 && battle.Round==3,"Guard halves the next hit");
        battle=new RpgTurnBattle(RpgState.CreateNew(false),48,false);
        check(!battle.Act(RpgBattleAction.Potion,1,1,1) && battle.Round==1 && battle.Player.health==100 && battle.Player.potions==3,"Invalid potion does not consume a turn or item");
        battle.Player.health=20;battle.Act(RpgBattleAction.Potion,1,1,1);
        check(battle.Player.health==71 && battle.Player.potions==2 && battle.Round==2,"Potion heals before the enemy's response");
        battle=new RpgTurnBattle(RpgState.CreateNew(false),10,false);battle.Act(RpgBattleAction.Attack,1,1,1);
        check(battle.Result==RpgBattleResult.Victory && battle.Player.health==100,"Defeated enemies cannot counterattack");
        check(!battle.Act(RpgBattleAction.Attack,1,1,1),"Finished fights reject more actions");
        var agile=RpgState.CreateNew(false);agile.unlockedSkills.Add(2);
        battle=new RpgTurnBattle(agile,48,false);battle.Act(RpgBattleAction.Attack,1,.1f,1);
        check(agile.health==100,"Unlocked dodge can avoid an enemy hit");
        var lucky=RpgState.CreateNew(false);lucky.unlockedSkills.Add(3);
        battle=new RpgTurnBattle(lucky,48,false);battle.Act(RpgBattleAction.Attack,.05f,1,1);
        check(battle.Result==RpgBattleResult.Victory && lucky.health==100,"Instant strike defeats regular enemies before their turn");
        battle=new RpgTurnBattle(lucky,220,true);battle.Act(RpgBattleAction.Attack,.05f,1,1);
        check(battle.Result==RpgBattleResult.Ongoing && battle.EnemyHealth==202 && lucky.health==78,"Bosses resist instant defeat");
        battle=new RpgTurnBattle(RpgState.CreateNew(false),48,false);battle.Act(RpgBattleAction.Escape,1,1,.2f);
        check(battle.Result==RpgBattleResult.Escaped && battle.Player.health==100,"Successful escape has no enemy response");
        battle=new RpgTurnBattle(RpgState.CreateNew(false),48,false);battle.Act(RpgBattleAction.Escape,1,1,.8f);
        check(battle.Result==RpgBattleResult.Ongoing && battle.Player.health==91 && battle.Round==2,"Failed escape allows one enemy response");
        battle.Player.health=1;battle.Act(RpgBattleAction.Guard,1,1,1);
        check(battle.Result==RpgBattleResult.Defeat && battle.Player.health==0,"Enemy turns can defeat the player");
        var quest=RpgState.CreateNew(false);quest.herbsQuest=1;quest.herbs=4;
        check(quest.FinishSideQuest(0) && quest.UpgradePoints==1 && !quest.FinishSideQuest(0),"Side quest awards one point once");
        quest.questStage=1;quest.banditsDefeated=5;quest.bossDefeated=true;
        check(quest.FinishQuest() && quest.UpgradePoints==2 && !quest.FinishQuest(),"Main quest awards one point once");
        var ritual=RpgState.CreateNew(false);ritual.cellarQuest=4;ritual.CollectCandles(false);ritual.ExplainRitual();
        check(ritual.UpgradePoints==1 && !ritual.ExplainRitual(),"Returning the candles grants one quest point");
        ritual.UpgradeAttribute(0);ritual.place=RpgPlace.ChurchRuins;var next=ritual.BeginSecondCycle();
        check(next.HasSkill(0) && next.health==120 && next.MaxHealth==120 && next.UpgradePoints==0,"Unlocked skills survive the story cycle");
        var legacy=new RpgState{level=8,experience=99,vitalityRank=2,herbsQuest=2};legacy.Normalize();
        check(legacy.level==1 && legacy.experience==0 && legacy.HasSkill(0) && legacy.UpgradePoints==1,"Legacy saves migrate completed quests and old upgrades");
        legacy.Normalize();check(legacy.UpgradePoints==1,"Migration cannot duplicate quest points");
        return count+CheckNpcRules();
    }
    public static int CheckNpcRules()
    {
        int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;};
        var s=RpgState.CreateNew(false);
        check(!s.NpcDefeated("mayor") && s.defeatedNpcs.Count==0,"Fresh game has all residents alive");
        check(s.DefeatNpc("mayor") && !s.DefeatNpc("mayor") && s.defeatedNpcs.Count==1,"NPC victories are recorded once");
        check(s.UpgradePoints==0 && s.gold==35 && s.banditsDefeated==0 && !s.bossDefeated,"NPC victories grant no quest points or hostile campaign progress");
        check(s.DefeatNpc("guard") && s.cellarQuest==1 && !s.NeedsGuideInstruction,"Defeated guide leaves the first clue without repeating portrait advice");
        check(s.DefeatNpc("elder") && s.cellarQuest==2,"Defeated elder leaves the shop clue");
        check(s.DefeatNpc("merchant") && s.cellarQuest==3 && s.CellarObjective.Contains("opuštěný"),"Defeated merchant leaves a usable cellar route");
        s.cellarQuest=4;check(s.CollectCandles(false) && s.CellarObjective.Contains("poznámky"),"Candles can be obtained without surviving story NPCs");
        check(s.ExplainRitual() && s.UpgradePoints==1 && !s.ExplainRitual(),"Guide notes unlock the ritual and award its quest point once");
        s.place=RpgPlace.ChurchRuins;var next=s.BeginSecondCycle();
        check(next.cycle==2 && next.defeatedNpcs.Count==0 && !next.NpcDefeated("guard"),"NPCs return in the next cycle");
        check(next.UpgradePoints==1 && s.defeatedNpcs.Count==4,"Cycle reset preserves points without sharing NPC records");
        var early=RpgState.CreateNew(false);early.DefeatNpc("elder");early.DefeatNpc("merchant");
        early.cellarQuest=1;early.ResolveNpcClues();check(early.cellarQuest==3,"Clues work when NPCs were defeated before accepting the guide quest");
        early.defeatedNpcs.Add("elder");early.defeatedNpcs.Add(null);early.Normalize();
        check(early.defeatedNpcs.Count==2,"Loading removes duplicate and empty NPC records");
        var named=new RpgTurnBattle(RpgState.CreateNew(false),72,false,"Petr");
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
        var state=RpgState.CreateNew(false);state.RewardQuest("a");state.UpgradeAttribute(3);state.RewardQuest("b");
        var saved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));saved.Normalize();
        if(saved.UpgradePoints!=1 || !saved.HasSkill(3) || saved.RewardQuest("a"))throw new Exception("Skill save round trip");
        state.DefeatNpc("healer");
        var npcSaved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));npcSaved.Normalize();
        if(!npcSaved.NpcDefeated("healer") || npcSaved.NpcDefeated("guard"))throw new Exception("NPC save round trip");
        Debug.Log("PASS: "+(count+2)+" turn combat, skill, reward and save checks.");
    }
#endif
}
