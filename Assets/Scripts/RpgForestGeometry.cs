using UnityEngine;

// Traced against the supplied 1154 x 769 forest paintings, in top-left image pixels.
// Walkable path corridors block dense woods, rocks and the river, leaving only the bridge crossing.
public static class RpgForestGeometry
{
    public const float Width=1154,Height=769;
    public static Vector3 World(float x,float y) => new Vector3(x/Width*32-16,12-y/Height*24,0);
    public static Vector2 Pixel(Vector2 p) => new Vector2((p.x+16)/32*Width,(12-p.y)/24*Height);
    public static readonly Vector2[] SafeTrail={
        new Vector2(180,735),new Vector2(200,690),new Vector2(265,615),new Vector2(240,555),
        new Vector2(195,500),new Vector2(220,470),new Vector2(305,462),new Vector2(390,452),
        new Vector2(470,458),new Vector2(535,456),new Vector2(510,425),new Vector2(500,395),
        new Vector2(505,365),new Vector2(540,335),new Vector2(560,316),new Vector2(645,320),
        new Vector2(735,335),new Vector2(820,332),new Vector2(885,300),new Vector2(955,285),
        new Vector2(1030,285),new Vector2(1045,285)
    };
    private static readonly Vector2[][] lightPaths={SafeTrail,
        new[]{new Vector2(1045,285),new Vector2(1090,267)},
        new[]{new Vector2(305,462),new Vector2(345,407),new Vector2(342,358),new Vector2(300,320),new Vector2(225,305),new Vector2(175,273),new Vector2(198,245),new Vector2(260,231),new Vector2(307,192),new Vector2(318,133),new Vector2(267,108)},
        new[]{new Vector2(560,316),new Vector2(600,280),new Vector2(625,240),new Vector2(660,209),new Vector2(665,164),new Vector2(651,130)},
        new[]{new Vector2(1030,285),new Vector2(1095,310),new Vector2(1110,360),new Vector2(1130,420),new Vector2(1140,470)}
    };
    private static readonly Vector2[][] darkPaths={
        new[]{new Vector2(180,735),new Vector2(195,680),new Vector2(235,615),new Vector2(268,566),new Vector2(300,524),new Vector2(320,483),new Vector2(350,458),new Vector2(380,445),new Vector2(390,420),new Vector2(410,389),new Vector2(470,376),new Vector2(515,365),new Vector2(560,386),new Vector2(620,400),new Vector2(665,415),new Vector2(735,425),new Vector2(800,399),new Vector2(850,375),new Vector2(900,347),new Vector2(955,344),new Vector2(1010,344),new Vector2(1070,325),new Vector2(1120,320)},
        new[]{new Vector2(390,420),new Vector2(375,365),new Vector2(345,332),new Vector2(320,309),new Vector2(290,275),new Vector2(300,238),new Vector2(330,194),new Vector2(320,164),new Vector2(270,145)},
        new[]{new Vector2(560,386),new Vector2(600,325),new Vector2(620,300),new Vector2(660,250),new Vector2(640,200),new Vector2(645,170)},
        new[]{new Vector2(1070,325),new Vector2(1110,380),new Vector2(1120,450),new Vector2(1140,500)}
    };
    public static readonly Rect[] LightSolids={new Rect(390,155,65,62),new Rect(620,82,65,33),new Rect(533,360,56,44),new Rect(760,254,69,49),new Rect(815,493,91,81),new Rect(318,372,12,22),new Rect(204,347,43,47),new Rect(480,335,18,20)};
    public static readonly Rect[] DarkSolids={new Rect(399,235,55,46),new Rect(616,119,61,34),new Rect(760,301,67,47),new Rect(814,511,80,65),new Rect(290,381,63,50),new Rect(518,320,46,53)};
    public static Rect[] Solids(RpgPlace place) => place==RpgPlace.Forest?LightSolids:DarkSolids;
    public static Rect WorldRect(Rect p) {var lower=World(p.xMin,p.yMax);var upper=World(p.xMax,p.yMin);return new Rect(lower.x,lower.y,upper.x-lower.x,upper.y-lower.y);}
    public static bool Blocked(RpgPlace place,Vector2 world,float radius)
    {
        var p=Pixel(world);float inset=radius*Width/32;
        bool path=false;
        foreach(var route in place==RpgPlace.Forest?lightPaths:darkPaths)
            for(int i=1;i<route.Length;i++)if(Distance(p,route[i-1],route[i])<=25-inset){path=true;break;}
        if(!path)return true;
        foreach(var rect in Solids(place))
        {
            var expanded=rect;expanded.xMin-=inset;expanded.yMin-=inset;expanded.xMax+=inset;expanded.yMax+=inset;
            if(expanded.Contains(p))return true;
        }
        return false;
    }
    private static float Distance(Vector2 p,Vector2 a,Vector2 b)
    {var d=b-a;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;return Vector2.Distance(p,a+d*t);}
    public static Vector3 Entry => World(180,735);
    public static Vector3 Candle => World(1045,285);
    public static Vector3 DeeperExit => World(1090,267);
    public static Vector3 Wolf(RpgPlace place,int index) => place==RpgPlace.Forest?(index==0?World(285,210):World(650,190)):(index==0?World(300,238):World(645,190));
    public static Vector3 Bat(RpgPlace place) => place==RpgPlace.Forest?World(1110,360):World(1110,420);
    public static Vector3 NextSafeWaypoint(Vector2 player)
    {
        int next=1;float distance=float.MaxValue;
        for(int i=1;i<SafeTrail.Length;i++)
        {
            Vector2 a=World(SafeTrail[i-1].x,SafeTrail[i-1].y),b=World(SafeTrail[i].x,SafeTrail[i].y);
            float d=Distance(player,a,b);
            if(d<distance){distance=d;next=i;}
        }
        if(next<SafeTrail.Length-1 && Vector2.Distance(player,World(SafeTrail[next].x,SafeTrail[next].y))<.55f)next++;
        return World(SafeTrail[next].x,SafeTrail[next].y);
    }
}
