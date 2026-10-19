using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

public static class RpgArchiveValidation
{
    public static int CheckDiscovery()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        for(int category=0;category<4;category++)check(RpgArchive.Entries(null,category).Count==0,"No story means an empty archive");
        var state=RpgState.CreateNew(false);
        for(int category=0;category<4;category++)check(RpgArchive.Entries(state,category).Count==0,"Creating a state does not reveal undiscovered entries");
        state.RecordArchiveNpc("healer");
        check(RpgArchive.Entries(state,0).Count==1 && !RpgArchive.Entries(state,0)[0].Contains("MIRA"),"An incomplete introduction records an encounter without revealing the name");
        state.MeetNpc("healer");state.MeetNpc("bandit0");
        var entries=RpgArchive.Entries(state,0);
        check(entries.Count==2 && entries[0].Contains("MIRA") && entries[1].Contains("PEKAŘ"),"Only introduced NPCs appear, including residents with legacy spawn IDs");
        check(RpgArchive.Entries(state,1).Count==0,"Meeting a resident does not discover a hostile enemy");
        check(state.RecordArchiveEnemy(false) && !state.RecordArchiveEnemy(false),"Enemy encounters are recorded once");
        entries=RpgArchive.Entries(state,1);
        check(entries.Count==1 && entries[0].Contains("VLK"),"Meeting a wolf does not reveal a bat");
        state.RecordArchiveEnemy(true);check(RpgArchive.Entries(state,1).Count==2,"Bat appears after a real encounter");
        state.archiveEnemies.Add("bandit");state.archiveEnemies.Add("boss");
        check(RpgArchive.Entries(state,1).Count==2,"Legacy enemy records cannot add a third type");
        state.Normalize();check(state.archiveEnemies.Count==2 && !state.archiveEnemies.Contains("boss"),"Loading removes obsolete enemy types");
        state.RecordArchivePlace(RpgArchive.PlaceKey(RpgPlace.Town));state.RecordArchivePlace(RpgArchive.PlaceKey(RpgPlace.Forest));
        check(!state.RecordArchivePlace(RpgArchive.PlaceKey(RpgPlace.Forest)),"Revisiting does not duplicate archive places");
        entries=RpgArchive.Entries(state,2);
        check(entries.Count==2 && !string.Join("",entries).Contains("KOSTEL"),"Only visited locations appear");
        state.RecordArchivePlace("shop");state.RecordArchivePlace("cellar");check(RpgArchive.Entries(state,2).Count==4,"Shop and cellar are separate visited interiors");
        state.RecordOwnedArchiveItems();check(RpgArchive.Entries(state,3).Count==2,"Owned consumables are known after play begins");
        check(!state.archiveItems.Contains("candle") && !state.archiveItems.Contains("herb") && !state.archiveItems.Contains("parcel"),"Unobtained quest items stay hidden");
        state.candles=1;state.RecordOwnedArchiveItems();state.candles=0;state.RecordOwnedArchiveItems();
        check(state.archiveItems.Contains("candle") && RpgArchive.Entries(state,3).Count==3,"Discovered items remain known after being consumed");
        state.cellarQuest=4;state.CollectCandles(false);state.ExplainRitual();state.place=RpgPlace.ChurchRuins;
        var second=state.BeginSecondCycle();
        check(second.archivePlaces.Count==0 && second.archiveItems.Count==0 && second.archiveEnemies.Count==0 && second.metNpcs.Count==0,"A new cycle starts fresh discovery records");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Archive Discovery")]
    public static void Run()
    {
        int count=CheckDiscovery();var state=RpgState.CreateNew(false);
        state.RecordArchiveEnemy(false);state.RecordArchivePlace("shop");state.RecordOwnedArchiveItems();state.MeetNpc("healer");
        var loaded=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));loaded.Normalize();
        for(int category=0;category<4;category++)
            if(string.Join("\n",RpgArchive.Entries(state,category))!=string.Join("\n",RpgArchive.Entries(loaded,category)))throw new Exception("Archive save round trip");
        Debug.Log("PASS: "+(count+4)+" archive discovery and save checks.");
    }
#endif
}
