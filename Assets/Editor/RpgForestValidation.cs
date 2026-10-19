using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public static class RpgForestValidation
{
    public static int CheckPaths()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        var trail=RpgForestGeometry.SafeTrail;
        for(int i=1;i<trail.Length;i++)
        {
            var a=RpgForestGeometry.World(trail[i-1].x,trail[i-1].y);var b=RpgForestGeometry.World(trail[i].x,trail[i].y);
            var midpoint=Vector3.Lerp(a,b,.55f);
            check(Vector2.Dot(RpgForestGeometry.NextSafeWaypoint(midpoint)-midpoint,b-a)>0,"Safe-route navigation points back instead of forward on segment "+i);
            int steps=Mathf.CeilToInt(Vector2.Distance(a,b)/.08f);
            for(int j=0;j<=steps;j++)
            {
                var p=Vector3.Lerp(a,b,(float)j/steps);
                check(!RpgLocations.Blocked(RpgPlace.Forest,p),"Safe forest path is obstructed near "+RpgForestGeometry.Pixel(p));
                check(Vector2.Distance(p,RpgForestGeometry.Wolf(RpgPlace.Forest,0))>1.8f && Vector2.Distance(p,RpgForestGeometry.Wolf(RpgPlace.Forest,1))>1.8f && Vector2.Distance(p,RpgForestGeometry.Bat(RpgPlace.Forest))>1.8f,"Recommended route enters enemy encounter range");
            }
            check(Vector3.Distance(RpgLocations.Move(RpgPlace.Forest,a,b),b)<.03f,"Player cannot follow the safe route segment "+i);
        }
        foreach(var place in new[]{RpgPlace.Forest,RpgPlace.DarkForest})
        {
            check(!RpgLocations.Blocked(place,RpgForestGeometry.Entry),"Forest entry is obstructed");
            for(int i=0;i<2;i++)check(!RpgLocations.Blocked(place,RpgForestGeometry.Wolf(place,i)),"Wolf spawn is inside forest scenery "+place+" "+i);
            check(!RpgLocations.Blocked(place,RpgForestGeometry.Bat(place)),"Bat spawn is inside forest scenery");
            var water=RpgForestGeometry.World(955,210);
            check(RpgLocations.Blocked(place,water),"River is walkable away from its bridge");
            check(RpgLocations.Blocked(place,RpgForestGeometry.World(425,260)),"Rock or dense woods are walkable");
        }
        check(!RpgLocations.Blocked(RpgPlace.Forest,RpgForestGeometry.Candle),"Forest candle is inaccessible");
        check(!RpgLocations.Blocked(RpgPlace.Library,new Vector3(0,6.3f)),"Librarian is inside a bookcase");
        var s=RpgState.CreateNew(false);s.forestRouteKnown=true;s.cellarQuest=4;s.CollectCandles(false);s.ExplainRitual();s.place=RpgPlace.ChurchRuins;
        check(s.BeginSecondCycle().forestRouteKnown,"Hero forgets the learned safe route between cycles");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Forest Paths and Safe Route")]
    public static void Run()=>Debug.Log("PASS: "+CheckPaths()+" forest collision and encounter checks.");
#endif
}
