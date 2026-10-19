using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
public static class RpgCycleValidation
{
    public static int CheckProgression()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        check(RpgEnemy.KindName(false)=="Vlk" && RpgEnemy.KindName(true)=="Netopýr","World and final wave share the two enemy names");
        var s=RpgState.CreateNew(false);
        check(!s.CanFight(false) && !s.CanFight(true) && !s.LearnCombat(false,false),"First cycle cannot learn or use normal combat");
        check(!s.ForestUnlocked && !s.CanAccessLocation(RpgPlace.Forest) && !s.CanAccessLocation(RpgPlace.DarkForest) && s.CanAccessLocation(RpgPlace.Library),"First cycle locks both forest areas but leaves the library open");
        check(!s.CanSpawnForestEnemy(false) && !s.CanSpawnForestEnemy(true),"First cycle spawns no forest enemies");
        var oldForest=new RpgState{place=RpgPlace.DarkForest};oldForest.Normalize();
        check(oldForest.place==RpgPlace.Town && oldForest.x==.5f && oldForest.y==3.5f,"Old first-cycle forest save returns safely to town");
        var battle=new RpgTurnBattle(s,48,false);
        check(!battle.Act(RpgBattleAction.Attack,1,1,1) && !battle.Act(RpgBattleAction.Guard,1,1,1) && !battle.Act(RpgBattleAction.Potion,1,1,1) && battle.Round==1,"Untrained player can only escape");
        check(battle.Act(RpgBattleAction.Escape,1,1,0) && battle.Result==RpgBattleResult.Escaped,"Untrained player can escape");
        check(s.RewardQuest("first") && !s.RewardQuest("extra") && s.UpgradePoints==1,"First cycle awards at most one quest point");
        s.cellarQuest=4;s.CollectCandles(false);s.ExplainRitual();s.place=RpgPlace.ChurchRuins;
        var next=s.BeginSecondCycle();
        check(next.cycle==2 && next.UpgradePoints==1 && next.RequiredCandles==4 && next.RitualShape=="čtverec","Second cycle carries unused point and requires four candles");
        check(next.ForestUnlocked && next.CanAccessLocation(RpgPlace.Forest) && next.CanAccessLocation(RpgPlace.DarkForest) && !next.forestDiscoverySeen,"Second cycle opens forest areas and starts the discovery dialogue unseen");
        check(next.CanSpawnForestEnemy(false) && !next.CanSpawnForestEnemy(true),"Second cycle forest contains wolves only");
        next.forestDiscoverySeen=true;next.Normalize();check(next.forestDiscoverySeen,"Forest discovery is not repeated after normalization");
        foreach(int stage in new[]{0,1,2,6}){next.cellarQuest=stage;check(!next.NeedsGuideInstruction,"Second cycle must not trigger automatic guide dialogue at stage "+stage);}
        next.cellarQuest=0;
        check(!next.CellarObjective.Contains("Promluv s Petrem") && !next.CellarObjective.Contains("dědy"),"Second cycle objective does not force the introduction chain");
        var independent=new RpgState{cycle=2,cellarQuest=4};
        check(independent.CollectCandles(false) && independent.ritualKnown && independent.cellarQuest==7 && independent.UpgradePoints==1 && !independent.HasMetNpc("guard"),"Later cycle remembers the ritual and completes candle retrieval without visiting Petr");
        var oldIndependent=new RpgState{cycle=2,cellarQuest=6,candles=3};oldIndependent.Normalize();oldIndependent.Normalize();
        check(oldIndependent.ritualKnown && oldIndependent.cellarQuest==7 && oldIndependent.UpgradePoints==1,"Old later-cycle candle saves migrate to independent progress once");
        check(next.LearnCombat(false,false) && !next.LearnCombat(false,false) && next.gold==35 && !next.CanFight(false),"Free sword training is permanent and still needs a sword");
        next.gold=100;next.BuyIronSword();check(next.CanSwordFight && !next.CanFight(true),"Sword cannot hit flying enemies");
        var flying=new RpgTurnBattle(next,40,false,"Flying",true);
        check(!flying.Act(RpgBattleAction.Attack,1,1,1),"Flying enemies reject melee");
        check(next.CollectExtraCandle(false) && !next.CollectExtraCandle(false) && next.CollectExtraCandle(true),"Extra sources are once per cycle");
        next.cellarQuest=4;check(next.CollectCandles(false) && next.candles==5 && !next.CollectCandles(false),"Cellar adds three even after extra candles");
        check(next.ritualKnown && !next.ExplainRitual() && next.PetrQuestComplete && next.DefeatNpc("guard") && next.candles==6,"Six optional candles include Petr after his quest");
        check(next.CyclePointsAwarded==3 && !next.RewardQuest("extra"),"Second cycle awards three quest points maximum");
        next.place=RpgPlace.ChurchRuins;var third=next.BeginSecondCycle();
        check(third.cycle==3 && third.RequiredCandles==5 && third.RitualShape=="hvězda" && third.swordTrained && !third.hasIronSword && !third.NpcDefeated("guard"),"Third cycle keeps training but resets equipment and revives Petr");
        check(third.CanSpawnForestEnemy(false) && third.CanSpawnForestEnemy(true) && !third.forestDiscoverySeen,"Third cycle contains wolves and bats and has a fresh entry notice");
        check(third.UpgradePoints==4 && third.candles==0 && !third.forestCandleTaken && !third.libraryCandleTaken,"Unused points persist, all candle sources reset");
        check(third.LearnCombat(true,false) && !third.LearnCombat(true,false) && !third.CanBowFight,"Bow training unlocks once and needs a bow");
        third.gold=100;check(third.BuyBow() && third.CanBowFight && third.CanFight(true),"Bought bow can fight flying enemies");
        flying=new RpgTurnBattle(third,40,false,"Flying",true);check(flying.Act(RpgBattleAction.Bow,1,1,1) && flying.EnemyHealth==24,"Bow hits flying enemy and advances one turn");
        var wolf=new RpgTurnBattle(third,40,false);
        check(!third.CanFight(false) && !wolf.Act(RpgBattleAction.Bow,1,1,1) && wolf.Round==1,"Bow alone cannot fight wolves or advance their turns");
        third.gold=40;third.BuyIronSword();
        check(third.CanFight(false) && !wolf.Act(RpgBattleAction.Bow,1,1,1) && wolf.Act(RpgBattleAction.Attack,1,1,1) && wolf.EnemyHealth==22,"Wolves accept sword attacks and reject bows even when both are owned");
        third.cellarQuest=4;third.CollectCandles(false);third.ExplainRitual();third.CollectExtraCandle(false);third.CollectExtraCandle(true);third.place=RpgPlace.ChurchRuins;
        check(third.CanPerformRitual && third.StartFinalWave() && !third.StartFinalWave() && third.finalEnemiesRemaining==RpgState.FinalWaveSize,"Final wave starts once");
        for(int i=0;i<RpgState.FinalWaveSize;i++)check(third.FinishFinalEnemy(),"Final wave counts a victory");
        check(third.storyComplete && third.CyclePointsAwarded==4 && !third.FinishFinalEnemy() && !third.CanPerformRitual && third.BeginSecondCycle()==null,"Final wave completes the story once and awards fourth point");
        third.Normalize();check(third.cycle==3 && third.storyComplete && third.hasBow && third.bowTrained,"Final progress survives normalization");
        var paid=RpgState.CreateNew(false);paid.cycle=2;paid.gold=29;check(!paid.LearnCombat(false,true) && paid.gold==29,"Paid training requires funds");
        paid.gold=30;check(paid.LearnCombat(false,true) && paid.gold==0,"Smith training costs thirty gold");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Three Cycle Progression")]
    public static void Run()
    {
        int checks=CheckProgression();var state=RpgState.CreateNew(false);state.cycle=3;state.swordTrained=state.bowTrained=state.hasBow=state.hasIronSword=true;state.place=RpgPlace.ChurchRuins;state.finalRitualStarted=true;state.finalEnemiesRemaining=5;state.upgradePoints=3;
        var loaded=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));loaded.Normalize();
        if(!loaded.swordTrained || !loaded.bowTrained || !loaded.hasBow || loaded.finalEnemiesRemaining!=5 || loaded.upgradePoints!=3)throw new Exception("Training, equipment, points and finale must survive save round trip");
        Debug.Log("PASS: "+(checks+1)+" cycle checks.");
    }
#endif
}
