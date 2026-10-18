using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public static class RpgRitualValidation
{
    public static int CheckStory()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        var fresh=RpgState.CreateNew(true);
        check(fresh.cycle==1 && fresh.candles==0 && !fresh.ritualKnown,"New story starts in the first cycle without candles");
        check(!fresh.CollectCandles(true) && !fresh.ExplainRitual() && fresh.BeginSecondCycle()==null,"Cannot skip the cellar or guide");
        var fled=new RpgState{cellarQuest=4};
        check(fled.candles==0 && fled.health==100 && fled.cellarQuest==4,"Fleeing preserves a repeatable cellar visit");
        check(!fled.ExplainRitual(),"Fleeing cannot unlock the ritual");
        var rush=new RpgState{cellarQuest=4};
        check(rush.CollectCandles(true) && rush.candles==3 && rush.health==80 && rush.cellarQuest==6,"Rushing gives three candles and costs 20 HP");
        check(!rush.CollectCandles(true) && rush.health==80 && rush.candles==3,"Cannot duplicate candles or cellar damage");
        var careful=new RpgState{cellarQuest=4};
        check(careful.CollectCandles(false) && careful.health==100 && careful.candles==3,"Careful approach gives candles without injury");
        careful.place=RpgPlace.ChurchRuins;
        check(!careful.CanPerformRitual && careful.BeginSecondCycle()==null,"Church requires the guide's instruction");
        int gold=careful.gold;
        check(careful.ExplainRitual() && !careful.ExplainRitual() && careful.gold==gold,"Guide unlocks the ritual once without the obsolete note reward");
        careful.place=RpgPlace.Library;
        check(!careful.CanPerformRitual,"Ritual must be in the church");
        careful.place=RpgPlace.ChurchRuins;careful.heroIsBoy=true;careful.health=12;
        careful.gold=999;careful.level=5;careful.collected.Add("chest");careful.deaths=2;
        check(careful.CanPerformRitual,"Prepared ritual becomes available");
        var second=careful.BeginSecondCycle();
        check(second.cycle==2 && second.ritualFailed && second.deaths==3 && second.heroIsBoy,"Ritual death advances cycle and preserves appearance and memory");
        check(second.health==100 && second.stamina==100 && second.gold==35 && second.level==1 && second.candles==0,"Cycle restores initial attributes and empties candle inventory");
        check(second.place==RpgPlace.Town && second.cellarQuest==0 && !second.ritualKnown && second.collected.Count==0,"Cycle resets the location, quest and world pickups");
        check(!second.CanPerformRitual && second.BeginSecondCycle()==null && second.NeedsGuideInstruction,"Second cycle starts a fresh candle quest and cannot repeat the ritual automatically");
        var wounded=new RpgState{cellarQuest=4,health=5};
        check(wounded.CollectCandles(true) && wounded.health==1,"Cellar hit leaves a wounded player alive to return to the guide");
        var old=new RpgState{cellarQuest=7,cellarNoteOpened=true};old.Normalize();
        check(old.candles==3 && old.cellarQuest==6 && !old.ritualKnown,"Old note ending migrates to the guide's candle conversation");
        var previousCount=new RpgState{cellarQuest=7,candles=5,ritualKnown=true};previousCount.Normalize();
        check(previousCount.candles==3 && previousCount.ritualKnown && previousCount.cellarQuest==7,"Existing five-candle saves migrate to three candles without losing the ritual instruction");
        var oldStairs=new RpgState{cellarQuest=5};oldStairs.Normalize();
        check(oldStairs.cellarQuest==4 && oldStairs.candles==0,"Old staircase saves can re-enter the cellar choices");
        var ready=new RpgState{cellarQuest=7,candles=3,ritualKnown=true};ready.Normalize();
        check(ready.cellarQuest==7 && ready.ritualKnown,"Normalization preserves the new ritual progress");
        second.Normalize();
        check(second.cycle==2 && second.candles==0 && second.ritualFailed,"Normalization preserves the second cycle");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Candle Ritual Story")]
    public static void Run() => Debug.Log("PASS: "+CheckStory()+" candle and cycle checks.");
#endif
}
