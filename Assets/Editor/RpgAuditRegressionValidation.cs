using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public static class RpgAuditRegressionValidation
{
    public static int CheckRules()
    {
        int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;};
        var s=RpgState.CreateNew(false);s.cycle=3;s.swordTrained=true;s.gold=100;
        s.BuyIronSword();
        check(!s.HunterStashAvailable && !s.RecoverHunterBow() && !s.StudyHunterNotes(),"Living hunter's stash cannot be used");
        s.cellarQuest=4;s.CollectCandles(false);s.CollectExtraCandle(false);s.librarySecretOpened=true;s.place=RpgPlace.SecretLibrary;s.TakeLibraryCandle();s.place=RpgPlace.ChurchRuins;
        check(s.CanPerformRitual && !s.CanBeginFinalRitual && !s.StartFinalWave(),"Final ritual rejects a player without bow and training before trapping them");
        s.DefeatNpc("guard");s.DefeatNpc("districtRybář Oldřich");
        check(s.HunterStashAvailable && !s.CanBuyBowFrom("districtRybář Oldřich") && s.CanBuyBowFrom("districtKovář Mikuláš"),"Smith replaces the dead bow seller");
        s.DefeatNpc("districtKovář Mikuláš");
        check(!s.CanBuyBowFrom("districtKovář Mikuláš"),"A dead replacement merchant cannot trade");
        s.gold=39;check(!s.RecoverHunterBow() && s.gold==39,"Stash bow still requires its full purchase price");
        s.gold=40;check(s.RecoverHunterBow() && s.hasBow && s.gold==0 && !s.RecoverHunterBow(),"Stash supplies one paid bow even if all teachers and smith are dead");
        check(s.StudyHunterNotes() && s.CanBowFight && !s.StudyHunterNotes() && !s.HunterStashAvailable,"Notes preserve a way to learn archery and cannot be reused");
        check(s.StartFinalWave() && s.FinalWaveActive,"Recovered gear lets the final ritual proceed");
        s.FinishFinalEnemy();s.health=0;s.stamina=0;s.gold=7;s.RecoverAfterDefeat();
        check(s.place==RpgPlace.Town && s.x==.5f && s.y==3.5f && s.health==s.MaxHealth && s.stamina==s.MaxStamina,"Defeat always returns to town with full health and stamina");
        check(!s.FinalWaveActive && s.finalEnemiesRemaining==0 && s.gold==0 && s.deaths==1,"Defeat resets final defence and caps the gold penalty");
        check(s.cycle==3 && s.candles==6 && s.hasBow && s.bowTrained && s.NpcDefeated("guard"),"Ordinary defeat preserves cycle, candles, equipment, knowledge and NPC outcomes");
        s.place=RpgPlace.ChurchRuins;check(s.StartFinalWave() && s.finalEnemiesRemaining==RpgState.FinalWaveSize,"Retry starts the complete wave");
        s.place=RpgPlace.Town;s.Normalize();check(s.place==RpgPlace.ChurchRuins && s.FinalWaveActive,"Old saves resume active defence inside the church");
        s.hasBow=false;s.Normalize();check(!s.FinalWaveActive && s.finalEnemiesRemaining==0,"Legacy wave without required gear returns to preparation");
        var p=RpgState.CreateNew(false);p.cellarQuest=4;p.CollectCandles(false);p.ExplainRitual();
        check(p.AcceptSideQuest(2),"Parcel can be accepted while Petr lives");p.DefeatNpc("guard");
        check(p.ParcelRecipientMissing && !p.SideQuestReady(2),"Petr's death marks an outstanding parcel as undeliverable");
        p.trackedQuest=2;check(p.CancelUndeliverableParcel() && p.deliveryQuest==0 && p.trackedQuest==-1,"Returning the parcel removes the active quest and marker");
        check(!p.AcceptSideQuest(2) && !p.CancelUndeliverableParcel(),"A dead recipient cannot receive a new or repeated parcel");
        var delivered=RpgState.CreateNew(false);delivered.cellarQuest=4;delivered.CollectCandles(false);delivered.ExplainRitual();delivered.AcceptSideQuest(2);delivered.parcelDelivered=true;delivered.DefeatNpc("guard");
        check(delivered.SideQuestReady(2) && !delivered.CancelUndeliverableParcel(),"Delivery made before Petr died can still be rewarded");
        var skills=RpgState.CreateNew(false);skills.upgradePoints=8;
        for(int i=0;i<RpgState.SkillCount;i++)check(skills.UpgradeAttribute(i),"All eight different skills can be unlocked");
        check(skills.UpgradePoints==0 && skills.unlockedSkills.Count==8 && !skills.UpgradeAttribute(8),"Eight points have eight permanent unlocks");
        skills.stamina=120;skills.Normalize();check(skills.MaxStamina==120 && skills.stamina==120,"Endurance survives save normalization without clamping to 100");
        skills.cycle=3;skills.swordTrained=skills.bowTrained=skills.hasIronSword=skills.hasBow=true;
        var bow=new RpgTurnBattle(skills,100,false,"Bat",true);bow.Act(RpgBattleAction.Bow,1,1,1);
        check(bow.EnemyHealth==72,"Archery skill adds six damage on top of strong strike");
        skills.health=skills.MaxHealth;var block=new RpgTurnBattle(skills,100,false);block.Act(RpgBattleAction.Guard,1,.4f,1);
        check(skills.health==skills.MaxHealth,"Improved block fully prevents the next hit at a 40 percent roll");
        var escape=new RpgTurnBattle(skills,100,false);escape.Act(RpgBattleAction.Escape,1,1,.7f);
        check(escape.Result==RpgBattleResult.Escaped,"Escape skill permits a roll above the base 50 percent chance");
        skills.cycle=1;skills.cellarQuest=4;skills.CollectCandles(false);skills.ExplainRitual();skills.place=RpgPlace.ChurchRuins;
        var next=skills.BeginSecondCycle();
        check(next.unlockedSkills.Count==8 && next.health==120 && next.stamina==120,"Cycle restart preserves all eight skills and fills both resources");
        check(RpgState.CreateNew(false).unlockedSkills.Count==0,"A new story still clears every skill");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Audit Regressions")]
    public static void Run(){Debug.Log("Audit regressions: "+CheckRules()+" checks passed.");}
#endif
}
