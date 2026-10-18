using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RpgNpcValidation
{
    public static int CheckSpacing()
    {
        int checks=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
        check(!RpgNpcSpacing.CanStep(new Vector2(-1,0),new Vector2(-.4f,0),Vector2.zero),"Residents stop before overlapping");
        check(!RpgNpcSpacing.CanStep(new Vector2(-3,0),new Vector2(3,0),Vector2.zero),"Long frames cannot pass through a resident");
        check(RpgNpcSpacing.CanStep(new Vector2(-2,-1),new Vector2(2,-1),Vector2.zero),"Residents can pass alongside one another");
        check(RpgNpcSpacing.CanStep(new Vector2(.2f,0),new Vector2(.3f,0),Vector2.zero),"Existing overlaps can separate");
        check(!RpgNpcSpacing.CanStep(new Vector2(.2f,0),new Vector2(.1f,0),Vector2.zero),"Existing overlaps cannot get deeper");
        check(RpgNpcSpacing.CanStep(Vector2.zero,new Vector2(.1f,0),Vector2.zero),"Residents at the same point can start separating");
        check(RpgNpcSpacing.CanStep(new Vector2(2,0),new Vector2(2,0),Vector2.zero),"Waiting away from another actor stays valid");
        check(RpgNpcSpacing.CanStep(new Vector2(-2,1),new Vector2(-1,1),Vector2.zero),"Normal unobstructed walking stays available");
        var watch=new RpgNpcStuckWatch();watch.Reset(Vector2.zero);
        check(!watch.Tick(Vector2.zero,1) && !watch.Tick(Vector2.zero,1),"Standing blocked for exactly two seconds does not reverse early");
        check(watch.Tick(Vector2.zero,.01f),"Blocking longer than two seconds requests a new direction");
        watch.Reset(Vector2.zero);bool reversed=false;
        for(int i=1;i<=80;i++)reversed|=watch.Tick(new Vector2(i*.005f,0),.05f);
        check(!reversed,"Small frame steps accumulate into genuine walking progress");
        watch.Reset(Vector2.zero);reversed=false;
        for(int i=0;i<25;i++)reversed|=watch.Tick(new Vector2(i%2==0?.01f:-.01f,0),.1f);
        check(reversed,"Jittering in place still counts as being stuck");
        watch.Reset(Vector2.zero);watch.Tick(Vector2.zero,1.5f);watch.Reset(Vector2.zero);
        check(!watch.Tick(Vector2.zero,1.5f),"Intentional work and dialogue pauses reset the stuck timer");
        foreach(var name in new[]{"Strážný Bohumil","Kovář Mikuláš","Trhovkyně Alena","Mlynář Matěj","Rybář Oldřich","Selka Dorota","Pekař Ondřej","Petr Kříž"})
        {
            string role=name.Contains("Ondřej")?"villager":name.Contains("Petr")?"guard":"district";
            var points=RpgNpcPatrols.Points(role,name);float length=0;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 from=points[i],to=points[(i+1)%points.Length];length+=Vector2.Distance(from,to);
                for(int sample=0;sample<=10;sample++)
                    check(RpgTownExpansion.Walkable(Vector2.Lerp(from,to,sample/10f)),"Patrol follows a walkable street: "+name+" segment "+i);
            }
            check(length>300,"Patrol must cover more than a small local shuffle: "+name);
        }
        var stranger=RpgState.CreateNew(false);
        check(!stranger.HasMetNpc("healer") && stranger.MeetNpc("healer") && stranger.HasMetNpc("healer"),"Completing an introduction records the acquaintance");
        check(!stranger.MeetNpc("healer") && stranger.metNpcs.Count==1 && stranger.UpgradePoints==0,"Introductions do not repeat or award quest points");
        stranger.metNpcs.Add(null);stranger.metNpcs.Add("healer");stranger.Normalize();
        check(stranger.metNpcs.Count==1,"Loading normalizes introduction records");
        stranger.cellarQuest=4;stranger.CollectCandles(false);stranger.ExplainRitual();stranger.place=RpgPlace.ChurchRuins;
        var newCycle=stranger.BeginSecondCycle();
        check(!newCycle.HasMetNpc("healer") && stranger.HasMetNpc("healer"),"NPCs see the hero as a stranger again in a new cycle");
        return checks;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check NPC Rendering And Spacing")]
    public static void Run()
    {
        int count=CheckSpacing();
        foreach(string role in new[]{"mayor","healer","guard","villager","merchant"})
        {
            var first=MedievalNpcAppearance.Get(role);var second=MedievalNpcAppearance.Get(role);
            if(first!=second||first.texture.filterMode!=FilterMode.Point||first.texture.wrapMode!=TextureWrapMode.Clamp||first.texture.mipmapCount!=1)
                throw new Exception("NPC sprite must retain its cached native artwork: "+role);
            count++;
        }
        var acquaintance=RpgState.CreateNew(false);acquaintance.MeetNpc("merchant");
        var saved=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(acquaintance));saved.Normalize();
        if(!saved.HasMetNpc("merchant") || saved.HasMetNpc("healer") || saved.MeetNpc("merchant"))throw new Exception("Introductions must survive loading without repeating");
        count++;
        Debug.Log("PASS: "+count+" NPC sprite and spacing checks.");
    }
#endif
}
