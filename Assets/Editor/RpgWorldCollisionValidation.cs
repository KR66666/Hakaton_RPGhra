using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public static class RpgWorldCollisionValidation
{
    public static int CheckDoors()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;};
        var bounds=RpgWorldGeometry.ExpandedBounds(new Bounds(new Vector3(0,2,1),new Vector3(40,26.666666f,1)));
        var doors=new[]{RpgWorldDoors.Shop,RpgWorldDoors.Library,RpgWorldDoors.Church};
        var approaches=new[]{RpgWorldDoors.ShopApproach,RpgWorldDoors.LibraryApproach,RpgWorldDoors.ChurchApproach};
        for(int i=0;i<doors.Length;i++)
        {
            var door=RpgWorldGeometry.ExpandedPixelPosition(doors[i],bounds);var approach=RpgWorldGeometry.ExpandedPixelPosition(approaches[i],bounds);
            check(!RpgWorldGeometry.Blocked(approach,bounds,0),"Door return point is inside a wall: "+i);
            check(RpgWorldGeometry.Blocked(door,bounds,0),"Door painting should stay on its building rather than be moved onto the road");
            var stopped=RpgWorldGeometry.Move(approach,door,bounds);
            check(!RpgWorldGeometry.Blocked(stopped,bounds,0) && Vector2.Distance(stopped,door)>.1f,"Walking through a threshold bypasses the building hitbox");
            check(RpgEntranceRules.CanUse(stopped,door,(Vector2)approach),"Player cannot activate the actual door from its reachable threshold");
            Vector2 facing=(approach-door).normalized;
            check(!RpgEntranceRules.CanUse((Vector2)door-facing*.3f,door,(Vector2)approach),"Door can be entered through the back wall");
            Vector2 side=new Vector2(-facing.y,facing.x);
            check(!RpgEntranceRules.CanUse((Vector2)door+facing*.4f+side*.8f,door,(Vector2)approach),"Door can be entered through a side wall");
        }
        foreach(var exit in RpgLocations.Exits)
        {
            if(exit.from==RpgPlace.Town)
            {
                var approach=RpgWorldGeometry.ExpandedPixelPosition(exit.approach??exit.point,bounds);
                check(!RpgWorldGeometry.Blocked(approach,bounds,0),"Town entrance approach is obstructed");
            }
            else
            {
                var approach=(Vector3)(exit.approach??exit.point);
                check(!RpgLocations.Blocked(exit.from,approach),"Interior approach is obstructed: "+exit.from);
                check(RpgEntranceRules.CanUse(approach,exit.point,exit.approach),"Interior door is out of reach: "+exit.from);
            }
            if(exit.to==RpgPlace.Town)check(!RpgWorldGeometry.Blocked(RpgWorldGeometry.ExpandedPixelPosition(exit.arrival,bounds),bounds,0),"Return to town overlaps an obstacle");
            else check(!RpgLocations.Blocked(exit.to,exit.arrival),"Arrival overlaps an interior obstacle");
        }
        check(RpgWorldGeometry.FootRadius>=.16f,"Player footprint is too small to protect walls and corners");
        check(RpgTownExpansion.BoundaryAt(new Vector2(514,414)),"Red road outline is present in the collision mask");
        check(!RpgTownExpansion.Walkable(new Vector2(514,414)),"Movement cannot cross the red road outline");
        check(RpgTownExpansion.Walkable(new Vector2(525,434)),"The marked road remains walkable inside its outline");
        return count;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check World Collisions and Door Thresholds")]
    public static void Run()=>Debug.Log("PASS: "+CheckDoors()+" world collision and doorway checks.");
#endif
}
