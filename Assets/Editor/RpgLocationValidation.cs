using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RpgLocationValidation
{
    public static int CheckGeometry()
    {
        int count=0;
        Action<bool,string> check=(value,message)=>{if(!value)throw new Exception(message);count++;};
        check(RpgEntranceRules.IsEntrance("shop") && RpgEntranceRules.IsEntrance("locationExit") && RpgEntranceRules.IsEntrance("secretRoom") && !RpgEntranceRules.IsEntrance("mayor"),"Only location entrances use door interactions");
        check(RpgEntranceRules.InReach(Vector2.zero,new Vector2(1.3f,0)) && !RpgEntranceRules.InReach(Vector2.zero,new Vector2(1.4f,0)),"Shop and all location doors share one interaction distance");
        check(RpgEntranceRules.Prompt("Knihovna")=="[E] Vstoupit: Knihovna" && RpgEntranceRules.Prompt("Obchod")=="[E] Vstoupit: Obchod","Door prompts have the same wording");
        check(!RpgLocations.Blocked(RpgPlace.Library,new Vector3(13,8)),"Secret room entrance is reachable outside bookcase colliders");
        var miniView=RpgMiniMapView.View(new Vector2(.5f,.5f));
        check(Math.Abs(miniView.width-.4f)<.0001f && Math.Abs(miniView.height-.4f)<.0001f,"Minimap displays a 2.5x local crop");
        check(Vector2.Distance(RpgMiniMapView.Project(new Vector2(.5f,.5f),miniView),new Vector2(.5f,.5f))<.0001f,"Player is centered in the local minimap");
        var corner=RpgMiniMapView.View(Vector2.zero);
        check(corner.xMin==0 && corner.yMin==0,"Minimap crop stays inside the map at the lower edge");
        corner=RpgMiniMapView.View(Vector2.one);
        check(Math.Abs(corner.xMax-1)<.0001f && Math.Abs(corner.yMax-1)<.0001f,"Minimap crop stays inside the map at the upper edge");
        check(RpgMiniMapView.Project(Vector2.zero,miniView).x<0 && RpgMiniMapView.Project(Vector2.one,miniView).x>1,"Distant markers project outside the visible crop");
        check(RpgLocations.Normalize((RpgPlace)99)==RpgPlace.Town,"Unknown saved locations must fall back to town");
        check(new RpgState().place==RpgPlace.Town,"Old saves and new games default to town");
        check(RpgLocations.Find(RpgPlace.Town,RpgPlace.DarkForest)==null,"Dark forest is reached through the forest");
        var townBounds=RpgWorldGeometry.ExpandedBounds(new Bounds(new Vector3(0,2,1),new Vector3(40,26.666666f,1)));
        foreach(var link in RpgLocations.Exits)
        {
            check(RpgLocations.Find(link.to,link.from)!=null,"Every location transition needs a return: "+link.from+" -> "+link.to);
            if(link.from==RpgPlace.Town)
            {
                var point=RpgWorldGeometry.ExpandedPixelPosition(link.point,townBounds);
                var safe=RpgWorldGeometry.NearestWalkable(point,townBounds);
                check(!RpgWorldGeometry.Blocked(safe,townBounds,RpgWorldGeometry.FeetOffset)&&Vector2.Distance(point,safe)<1,"Town entrance must be close to visible paving: "+link.to);
            }
            else check(!RpgLocations.Blocked(link.from,link.point),"Exit is not inside an obstacle: "+link.from);
            if(link.to==RpgPlace.Town)
            {
                var point=RpgWorldGeometry.ExpandedPixelPosition(link.arrival,townBounds);
                check(Vector2.Distance(point,RpgWorldGeometry.NearestWalkable(point,townBounds))<1,"Return to town must be on a reachable road");
            }
            else
            {
                check(!RpgLocations.Blocked(link.to,link.arrival),"Arrival must be outside obstacles: "+link.to);
                foreach(var next in RpgLocations.Exits)if(next.from==link.to)
                    check(Reachable(link.to,link.arrival,next.point),"Walking route from arrival to exit: "+link.to+" -> "+next.to);
            }
        }
        foreach(var place in new[]{RpgPlace.Forest,RpgPlace.DarkForest,RpgPlace.Library,RpgPlace.ChurchRuins})
        {
            var obstacle=RpgLocations.Obstacles(place)[0];float y=obstacle.center.y;
            var start=new Vector3(obstacle.xMin-1,y,0);var end=new Vector3(obstacle.xMax+1,y,0);
            var stopped=RpgLocations.Move(place,start,end);
            check(stopped.x<obstacle.xMin-.15f,"Fast movement must not tunnel through obstacles: "+place);
            check(RpgLocations.Blocked(place,new Vector3(16,0,0)),"Map edges block movement: "+place);
            var recovered=RpgLocations.SafePosition(place,obstacle.center);
            check(!RpgLocations.Blocked(place,recovered),"A save inside an obstacle finds a safe position: "+place);
            var safe=RpgLocations.SafePosition(place,new Vector3(1000,-1000,0));
            check(!RpgLocations.Blocked(place,safe),"A save outside the map gets a safe fallback: "+place);
        }
        return count;
    }
    private static bool Reachable(RpgPlace place,Vector2 start,Vector2 end)
    {
        const float step=.5f;
        var origin=new Vector2Int(Mathf.RoundToInt(start.x/step),Mathf.RoundToInt(start.y/step));
        var goal=new Vector2Int(Mathf.RoundToInt(end.x/step),Mathf.RoundToInt(end.y/step));
        var queue=new Queue<Vector2Int>();var seen=new HashSet<Vector2Int>();queue.Enqueue(origin);seen.Add(origin);
        var offsets=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        while(queue.Count>0)
        {
            var node=queue.Dequeue();if(node==goal)return true;
            foreach(var offset in offsets)
            {
                var next=node+offset;
                if(Mathf.Abs(next.x)>31||Mathf.Abs(next.y)>23||seen.Contains(next))continue;
                Vector3 point=new Vector3(next.x*step,next.y*step,0);
                Vector3 moved=RpgLocations.Move(place,new Vector3(node.x*step,node.y*step,0),point);
                if(Vector3.Distance(moved,point)>.01f)continue;
                seen.Add(next);queue.Enqueue(next);
            }
        }
        return false;
    }
#if UNITY_EDITOR
    [MenuItem("Tools/RPG/Check Location Transitions")]
    public static void Run()
    {
        int count=CheckGeometry();
        foreach(var place in new[]{RpgPlace.Forest,RpgPlace.DarkForest,RpgPlace.Library,RpgPlace.ChurchRuins})
        {
            var saved=new RpgState{place=place,x=1,y=-7,health=38,gold=67,cellarQuest=5};
            var restored=JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(saved));restored.Normalize();
            if(restored.place!=place||restored.x!=1||restored.y!=-7||restored.health!=38||restored.gold!=67||restored.cellarQuest!=4)
                throw new Exception("Location save round trip failed: "+place);
            var map=RpgLocationArt.Map(place);
            if(map==null||map.texture.filterMode!=FilterMode.Point||Mathf.Abs(map.bounds.size.x-32)>.01f||Mathf.Abs(map.bounds.size.y-24)>.01f)
                throw new Exception("Location map dimensions failed: "+place);
            count+=2;
        }
        Debug.Log("PASS: "+count+" location, route, collision, artwork and save checks.");
    }
#endif
}
