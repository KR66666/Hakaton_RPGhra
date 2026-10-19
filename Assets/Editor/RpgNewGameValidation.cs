using System;
using System.Collections;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public static class RpgNewGameValidation
{
    public static int CheckFreshState()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        var male=RpgState.CreateNew(true);var female=RpgState.CreateNew(false);
        check(male.heroIsBoy&&!female.heroIsBoy,"Selected appearance must be preserved");
        foreach(var field in typeof(RpgState).GetFields(BindingFlags.Instance|BindingFlags.Public))
        {
            if(field.Name==nameof(RpgState.heroIsBoy) || field.Name==nameof(RpgState.heroName))continue;
            var a=field.GetValue(male);var b=field.GetValue(female);
            if(a is IList list)check(list.Count==0&&((IList)b).Count==0&&!ReferenceEquals(a,b),"Independent empty collection: "+field.Name);
            else check(Equals(a,b),"Equal starting value: "+field.Name);
        }
        check(male.MaxHealth==100&&female.MaxHealth==100&&male.MaxStamina==100&&female.MaxStamina==100,"Both appearances start with 100 HP and stamina");
        check(male.Damage==female.Damage&&male.Defense==female.Defense&&male.UpgradePoints==female.UpgradePoints,"Equal combat attributes and upgrade points");
        male.level=female.level=4;male.vitalityRank=female.vitalityRank=1;male.weaponLevel=female.weaponLevel=2;
        check(male.MaxHealth==female.MaxHealth&&male.Damage==female.Damage,"Upgrades work identically for both appearances");
        var old=RpgState.CreateNew(true);
        old.place=RpgPlace.DarkForest;old.level=12;old.experience=30;old.health=9;old.stamina=7;old.gold=9999;
        old.cellarQuest=7;old.guideSeenStage=7;old.cellarNoteTaken=old.cellarNoteOpened=true;
        old.collected.Add("chest1");old.defeated.Add("commander");old.completedAreas.Add(0);
        old.areaProgress.Add(new RpgAreaProgress{area=2,quest=2});old.travelCompanion=2;
        old.herbsQuest=old.deliveryQuest=old.suppliesQuest=2;old.vitalityRank=3;old.parcelDelivered=true;
        old.x=100;old.y=-80;old.gardenReadyAt=long.MaxValue;
        var pending=typeof(MainMenu).GetField("pendingGame",BindingFlags.Static|BindingFlags.NonPublic);
        var flag=typeof(MainMenu).GetField("pendingNewGame",BindingFlags.Static|BindingFlags.NonPublic);
        object originalPending=pending.GetValue(null),originalFlag=flag.GetValue(null);
        bool? originalSelection=MainMenu.SelectedHeroIsBoy;bool originalTest=MainMenu.LaunchTest;
        try
        {
            pending.SetValue(null,old);flag.SetValue(null,false);MainMenu.LaunchTest=true;
            MainMenu.PrepareNewGame(false);
            var fresh=MainMenu.ConsumePendingGame(out bool isNew);
            check(isNew&&!MainMenu.LaunchTest&&!fresh.heroIsBoy,"New story overrides stale load and test requests");
            var defaults=RpgState.CreateNew(false);
            foreach(var field in typeof(RpgState).GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                object value=field.GetValue(fresh),expected=field.GetValue(defaults);
                if(value is IList list)check(list.Count==0&&!ReferenceEquals(value,field.GetValue(old)),"New game resets collection: "+field.Name);
                else check(Equals(value,expected),"New game resets field: "+field.Name);
            }
            check(old.level==12&&old.health==9&&old.collected.Count==1,"Fresh state must not share mutable data with the old story");
            check(MainMenu.ConsumePendingGame(out isNew)==null&&!isNew,"Launch state and new-game flag are consumed once");
            pending.SetValue(null,old);flag.SetValue(null,false);
            check(ReferenceEquals(MainMenu.ConsumePendingGame(out isNew),old)&&!isNew,"Explicit loading preserves the original saved state");
        }
        finally
        {
            pending.SetValue(null,originalPending);flag.SetValue(null,originalFlag);
            MainMenu.SelectedHeroIsBoy=originalSelection;MainMenu.LaunchTest=originalTest;
        }
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check New Game Reset")]
    public static void Run() => Debug.Log("PASS: "+CheckFreshState()+" fresh-game and equal-attribute checks.");
#endif
}
