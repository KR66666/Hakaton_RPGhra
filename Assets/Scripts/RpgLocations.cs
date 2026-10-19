using System;
using System.Collections.Generic;
using UnityEngine;

public enum RpgPlace { Town, Forest, DarkForest, Library, ChurchRuins, SecretLibrary }

public sealed class RpgLocationExit
{
    public readonly RpgPlace from, to;
    public readonly Vector2 point, arrival;
    public readonly Vector2? approach;
    public readonly string directions;
    public RpgLocationExit(RpgPlace from, RpgPlace to, Vector2 point, Vector2 arrival, string directions,Vector2? approach=null)
    { this.from=from;this.to=to;this.point=point;this.arrival=arrival;this.directions=directions;this.approach=approach; }
}

public static class RpgLocations
{
    public const float SecretRoomScale=.55f;
    private static readonly Dictionary<RpgPlace,List<Rect>> obstacleCache=new Dictionary<RpgPlace,List<Rect>>();
    // Town coordinates are pixels on MedievalTownExpanded; other maps use world units.
    public static readonly RpgLocationExit[] Exits = {
        new RpgLocationExit(RpgPlace.Library,RpgPlace.SecretLibrary,RpgWorldDoors.SecretPassage,RpgWorldDoors.SecretInsideApproach,"Skrytý průchod v regálu",RpgWorldDoors.SecretPassageApproach),
        new RpgLocationExit(RpgPlace.SecretLibrary,RpgPlace.Library,RpgWorldDoors.SecretInside,RpgWorldDoors.SecretPassageApproach,"Zpět do knihovny",RpgWorldDoors.SecretInsideApproach),
        new RpgLocationExit(RpgPlace.Town,RpgPlace.Forest,RpgWorldDoors.ForestGate,(Vector2)RpgForestGeometry.Entry,"Severní brána města"),
        new RpgLocationExit(RpgPlace.Forest,RpgPlace.Town,(Vector2)RpgForestGeometry.Entry,RpgWorldDoors.ForestArrival,"Jižní cesta zpět do Havranova"),
        new RpgLocationExit(RpgPlace.Forest,RpgPlace.DarkForest,(Vector2)RpgForestGeometry.DeeperExit,(Vector2)RpgForestGeometry.Entry,"Severní stezka hlouběji do lesa"),
        new RpgLocationExit(RpgPlace.DarkForest,RpgPlace.Forest,(Vector2)RpgForestGeometry.Entry,(Vector2)RpgForestGeometry.DeeperExit,"Jihozápadní stezka na okraj lesa"),
        new RpgLocationExit(RpgPlace.Town,RpgPlace.Library,RpgWorldDoors.Library,RpgWorldDoors.LibraryInsideApproach,"Vchod do knihovny v jižní části města",RpgWorldDoors.LibraryApproach),
        new RpgLocationExit(RpgPlace.Library,RpgPlace.Town,RpgWorldDoors.LibraryInside,RpgWorldDoors.LibraryApproach,"Dveře zpět do města",RpgWorldDoors.LibraryInsideApproach),
        new RpgLocationExit(RpgPlace.Town,RpgPlace.ChurchRuins,RpgWorldDoors.Church,new Vector2(0,-7.5f),"Cesta u kostela ve východní části města",RpgWorldDoors.ChurchApproach),
        new RpgLocationExit(RpgPlace.ChurchRuins,RpgPlace.Town,new Vector2(0,-9),RpgWorldDoors.ChurchApproach,"Jižní brána zříceniny"),
    };
    public static string Name(RpgPlace place) => place==RpgPlace.Forest?"Les":place==RpgPlace.DarkForest?"Temný les":
        place==RpgPlace.Library?"Knihovna":place==RpgPlace.SecretLibrary?"Tajný archiv":place==RpgPlace.ChurchRuins?"Zřícenina kostela":"Havranov";
    public static string Description(RpgPlace place) => place==RpgPlace.Forest?"Okraj lesa. Přes most na východě dojdeš do Temného lesa. Jihozápadní stezka vede zpět do města.":
        place==RpgPlace.DarkForest?"Hluboký, temný les. Na jihu najdeš cestu zpět na okraj lesa.":
        place==RpgPlace.Library?"Tichá knihovna s regály a čítárnou. Východ je v jižní stěně.":
        place==RpgPlace.SecretLibrary?"Zapomenutý archiv za tajným průchodem. Jižní dveře vedou zpět do knihovny.":
        place==RpgPlace.ChurchRuins?"Rozpadlé zdi, kamenné lavice a starý oltář. Jižní brána vede zpět do města.":
        "Město spojuje les, knihovnu a zříceninu kostela. Obchod se sklepem zůstává na svém místě.";
    public static RpgPlace Normalize(RpgPlace place) => (int)place>=0&&(int)place<=5?place:RpgPlace.Town;
    public static RpgLocationExit Find(RpgPlace from,RpgPlace to) => Array.Find(Exits,e=>e.from==from&&e.to==to);
    public static List<Rect> Obstacles(RpgPlace place)
    {
        if(obstacleCache.TryGetValue(place,out var cached))return cached;
        var result=new List<Rect>();
        if(place==RpgPlace.Forest||place==RpgPlace.DarkForest)
        {
            foreach(var solid in RpgForestGeometry.Solids(place))result.Add(RpgForestGeometry.WorldRect(solid));
        }
        else if(place==RpgPlace.Library)
        {
            // Footprints match the sixteen shelves on LibraryWalkableMap (1536 x 1024).
            int[] xs={164,462,828,1147},widths={231,247,247,231};
            int[] ys={216,370,545,714},heights={116,119,132,127};
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
                result.Add(LibraryPixelRect(xs[col],ys[row],widths[col],heights[row]));
            result.Add(LibraryPixelRect(0,0,1536,155));
            result.Add(LibraryPixelRect(665,155,210,49)); // Librarian's desk.
            result.Add(LibraryPixelRect(0,155,91,755));
            result.Add(LibraryPixelRect(1440,155,96,755));
            result.Add(LibraryPixelRect(0,910,700,114));
            result.Add(LibraryPixelRect(840,910,696,114));
        }
        else if(place==RpgPlace.SecretLibrary)
        {
            result.Add(new Rect(-10.35f,.8f,1.7f,.65f)); // The small candle shelf.
            result.Add(new Rect(-16,5,32,7));
            result.Add(new Rect(-16,-12,4.3f,24));result.Add(new Rect(11.7f,-12,4.3f,24));
            result.Add(new Rect(-16,-12,13.6f,4.4f));result.Add(new Rect(2.4f,-12,13.6f,4.4f));
        }
        else if(place==RpgPlace.ChurchRuins)
        {
            result.Add(new Rect(-10,-6,1,12));result.Add(new Rect(9,-6,1,7));result.Add(new Rect(9,3,1,3));
            result.Add(new Rect(-10,6,7,1));result.Add(new Rect(2,6,8,1));result.Add(new Rect(-2,4,4,1.5f));
            foreach(int y in new[]{-3,0,3}){result.Add(new Rect(-7,y,4,1));result.Add(new Rect(3,y,4,1));}
        }
        if(place==RpgPlace.SecretLibrary)
            for(int i=0;i<result.Count;i++){var rect=result[i];result[i]=new Rect(rect.position*SecretRoomScale,rect.size*SecretRoomScale);}
        obstacleCache[place]=result;return result;
    }
    private static Rect LibraryPixelRect(float x,float y,float width,float height) =>
        new Rect(x/48-16,12-(y+height)*24/1024,width/48,height*24/1024);
    public static bool Blocked(RpgPlace place,Vector3 position,float radius=RpgWorldGeometry.FootRadius)
    {
        float scale=place==RpgPlace.SecretLibrary?SecretRoomScale:1;
        if(Mathf.Abs(position.x)>15.5f*scale-radius||Mathf.Abs(position.y)>11.5f*scale-radius)return true;
        if((place==RpgPlace.Forest || place==RpgPlace.DarkForest) && RpgForestGeometry.Blocked(place,position,radius))return true;
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
        if(place==RpgPlace.Forest || place==RpgPlace.DarkForest)return RpgForestGeometry.Entry;
        return new Vector3(0,-7.5f*(place==RpgPlace.SecretLibrary?SecretRoomScale:1),position.z);
    }
}
