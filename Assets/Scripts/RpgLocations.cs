using System;
using System.Collections.Generic;
using UnityEngine;

public enum RpgPlace { Town, Forest, DarkForest, Library, ChurchRuins }

public sealed class RpgLocationExit
{
    public readonly RpgPlace from, to;
    public readonly Vector2 point, arrival;
    public readonly string directions;
    public RpgLocationExit(RpgPlace from, RpgPlace to, Vector2 point, Vector2 arrival, string directions)
    { this.from=from;this.to=to;this.point=point;this.arrival=arrival;this.directions=directions; }
}

public static class RpgLocations
{
    private static readonly Dictionary<RpgPlace,List<Rect>> obstacleCache=new Dictionary<RpgPlace,List<Rect>>();
    // Town coordinates are pixels on MedievalTownExpanded; other maps use world units.
    public static readonly RpgLocationExit[] Exits = {
        new RpgLocationExit(RpgPlace.Town,RpgPlace.Forest,new Vector2(760,140),new Vector2(0,-7.5f),"Severní brána města"),
        new RpgLocationExit(RpgPlace.Forest,RpgPlace.Town,new Vector2(0,-9),new Vector2(760,170),"Jižní cesta zpět do Havranova"),
        new RpgLocationExit(RpgPlace.Forest,RpgPlace.DarkForest,new Vector2(0,9),new Vector2(0,-7.5f),"Severní stezka hlouběji do lesa"),
        new RpgLocationExit(RpgPlace.DarkForest,RpgPlace.Forest,new Vector2(0,-9),new Vector2(0,7.5f),"Jižní stezka na okraj lesa"),
        new RpgLocationExit(RpgPlace.Town,RpgPlace.Library,new Vector2(650,650),new Vector2(0,-7.5f),"Vchod do knihovny v jižní části města"),
        new RpgLocationExit(RpgPlace.Library,RpgPlace.Town,new Vector2(0,-9),new Vector2(650,665),"Dveře zpět do města"),
        new RpgLocationExit(RpgPlace.Town,RpgPlace.ChurchRuins,new Vector2(1045,395),new Vector2(0,-7.5f),"Cesta u kostela ve východní části města"),
        new RpgLocationExit(RpgPlace.ChurchRuins,RpgPlace.Town,new Vector2(0,-9),new Vector2(1045,415),"Jižní brána zříceniny"),
    };
    public static string Name(RpgPlace place) => place==RpgPlace.Forest?"Les":place==RpgPlace.DarkForest?"Temný les":
        place==RpgPlace.Library?"Knihovna":place==RpgPlace.ChurchRuins?"Zřícenina kostela":"Havranov";
    public static string Description(RpgPlace place) => place==RpgPlace.Forest?"Okraj lesa. Severní stezka vede do Temného lesa, jižní zpět do města.":
        place==RpgPlace.DarkForest?"Hluboký, temný les. Na jihu najdeš cestu zpět na okraj lesa.":
        place==RpgPlace.Library?"Tichá knihovna s regály a čítárnou. Východ je v jižní stěně.":
        place==RpgPlace.ChurchRuins?"Rozpadlé zdi, kamenné lavice a starý oltář. Jižní brána vede zpět do města.":
        "Město spojuje les, knihovnu a zříceninu kostela. Obchod se sklepem zůstává na svém místě.";
    public static RpgPlace Normalize(RpgPlace place) => (int)place>=0&&(int)place<=4?place:RpgPlace.Town;
    public static RpgLocationExit Find(RpgPlace from,RpgPlace to) => Array.Find(Exits,e=>e.from==from&&e.to==to);
    public static List<Rect> Obstacles(RpgPlace place)
    {
        if(obstacleCache.TryGetValue(place,out var cached))return cached;
        var result=new List<Rect>();
        if(place==RpgPlace.Forest||place==RpgPlace.DarkForest)
        {
            for(int y=-8;y<=8;y+=4)for(int x=-12;x<=12;x+=4)
                if(Mathf.Abs(x)>2&&Mathf.Abs(y)>1)result.Add(new Rect(x-.8f,y-.8f,1.6f,1.6f));
        }
        else if(place==RpgPlace.Library)
        {
            foreach(int x in new[]{-11,-6,6,11})foreach(int y in new[]{-4,0,4})result.Add(new Rect(x-1.4f,y-.65f,2.8f,1.3f));
            result.Add(new Rect(-1.8f,4,3.6f,1.8f));
        }
        else if(place==RpgPlace.ChurchRuins)
        {
            result.Add(new Rect(-10,-6,1,12));result.Add(new Rect(9,-6,1,7));result.Add(new Rect(9,3,1,3));
            result.Add(new Rect(-10,6,7,1));result.Add(new Rect(2,6,8,1));result.Add(new Rect(-2,4,4,1.5f));
            foreach(int y in new[]{-3,0,3}){result.Add(new Rect(-7,y,4,1));result.Add(new Rect(3,y,4,1));}
        }
        obstacleCache[place]=result;return result;
    }
    public static bool Blocked(RpgPlace place,Vector3 position,float radius=.2f)
    {
        if(Mathf.Abs(position.x)>15.5f-radius||Mathf.Abs(position.y)>11.5f-radius)return true;
        foreach(var rect in Obstacles(place))
        {
            float x=Mathf.Clamp(position.x,rect.xMin,rect.xMax),y=Mathf.Clamp(position.y,rect.yMin,rect.yMax);
            if((position.x-x)*(position.x-x)+(position.y-y)*(position.y-y)<radius*radius)return true;
        }
        return false;
    }
    public static Vector3 Move(RpgPlace place,Vector3 current,Vector3 desired)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(current,desired)/.06f));
        var delta=(desired-current)/steps;
        for(int i=0;i<steps;i++)
        {
            var next=current+new Vector3(delta.x,0,0);if(!Blocked(place,next))current.x=next.x;
            next=current+new Vector3(0,delta.y,0);if(!Blocked(place,next))current.y=next.y;
        }
        return current;
    }
    public static Vector3 SafePosition(RpgPlace place,Vector3 position)
    {
        if(!Blocked(place,position))return position;
        for(float radius=.25f;radius<=32;radius+=.25f)for(int i=0;i<32;i++)
        {
            var candidate=position+new Vector3(Mathf.Cos(i*Mathf.PI/16),Mathf.Sin(i*Mathf.PI/16),0)*radius;
            if(!Blocked(place,candidate))return candidate;
        }
        return new Vector3(0,-7.5f,position.z);
    }
}
