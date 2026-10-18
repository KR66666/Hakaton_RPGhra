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
        var battle=new RpgTurnBattle(s,48,false);
        check(!battle.Act(RpgBattleAction.Attack,1,1,1) && !battle.Act(RpgBattleAction.Guard,1,1,1) && !battle.Act(RpgBattleAction.Potion,1,1,1) && battle.Round==1,"Untrained player can only escape");
        check(battle.Act(RpgBattleAction.Escape,1,1,0) && battle.Result==RpgBattleResult.Escaped,"Untrained player can escape");
        check(s.RewardQuest("first") && !s.RewardQuest("extra") && s.UpgradePoints==1,"First cycle awards at most one quest point");
        s.cellarQuest=4;s.CollectCandles(false);s.ExplainRitual();s.place=RpgPlace.ChurchRuins;
        var next=s.BeginSecondCycle();
        check(next.cycle==2 && next.UpgradePoints==1 && next.RequiredCandles==4 && next.RitualShape=="čtverec","Second cycle carries unused point and requires four candles");
        check(next.LearnCombat(false,false) && !next.LearnCombat(false,false) && next.gold==35 && !next.CanFight(false),"Free sword training is permanent and still needs a sword");
        next.gold=100;next.BuyIronSword();check(next.CanSwordFight && !next.CanFight(true),"Sword cannot hit flying enemies");
        var flying=new RpgTurnBattle(next,40,false,"Flying",true);
        check(!flying.Act(RpgBattleAction.Attack,1,1,1),"Flying enemies reject melee");
        check(next.CollectExtraCandle(false) && !next.CollectExtraCandle(false) && next.CollectExtraCandle(true),"Extra sources are once per cycle");
        next.cellarQuest=4;check(next.CollectCandles(false) && next.candles==5 && !next.CollectCandles(false),"Cellar adds three even after extra candles");
        check(next.ExplainRitual() && next.PetrQuestComplete && next.DefeatNpc("guard") && next.candles==6,"Six optional candles include Petr after his quest");
        check(next.CyclePointsAwarded==3 && !next.RewardQuest("extra"),"Second cycle awards three quest points maximum");
        next.place=RpgPlace.ChurchRuins;var third=next.BeginSecondCycle();
        check(third.cycle==3 && third.RequiredCandles==5 && third.RitualShape=="hvězda" && third.swordTrained && !third.hasIronSword && !third.NpcDefeated("guard"),"Third cycle keeps training but resets equipment and revives Petr");
        check(third.UpgradePoints==4 && third.candles==0 && !third.forestCandleTaken && !third.libraryCandleTaken,"Unused points persist, all candle sources reset");
        check(third.LearnCombat(true,false) && !third.LearnCombat(true,false) && !third.CanBowFight,"Bow training unlocks once and needs a bow");
        third.gold=100;check(third.BuyBow() && third.CanBowFight && third.CanFight(true),"Bought bow can fight flying enemies");
        flying=new RpgTurnBattle(third,40,false,"Flying",true);check(flying.Act(RpgBattleAction.Bow,1,1,1) && flying.EnemyHealth==24,"Bow hits flying enemy and advances one turn");
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
        int checks=CheckProgression();var state=RpgState.CreateNew(false);state.cycle=3;state.swordTrained=state.bowTrained=state.hasBow=true;state.finalRitualStarted=true;state.finalEnemiesRemaining=5;state.upgradePoints=3;
        var loaded=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));loaded.Normalize();
        if(!loaded.swordTrained || !loaded.bowTrained || !loaded.hasBow || loaded.finalEnemiesRemaining!=5 || loaded.upgradePoints!=3)throw new Exception("Training, equipment, points and finale must survive save round trip");
        Debug.Log("PASS: "+(checks+1)+" cycle checks.");
    }
#endif
}
