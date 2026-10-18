using UnityEngine;

// Native 1536 x 1024 coordinates traced on MedievalTownExpanded.png.
public static class RpgTownExpansion
{
    private struct Lane
    {
        public Vector2 a,b;public float radius;
        public Lane(float x,float y,float xx,float yy,float r){a=new Vector2(x,y);b=new Vector2(xx,yy);radius=r;}
    }
    private static readonly Lane[] lanes={
        new Lane(345,434,1100,434,20),new Lane(1100,434,1300,434,18),new Lane(1300,434,1440,450,18),new Lane(1440,450,1531,445,18),
        new Lane(780,180,780,810,20),new Lane(650,350,650,434,16),new Lane(460,310,460,434,16),
        new Lane(1045,350,1045,434,16),new Lane(860,282,860,355,16),new Lane(780,355,925,355,17),
        new Lane(525,434,525,675,17),new Lane(525,675,1060,675,18),new Lane(1060,434,1060,675,17),
        new Lane(650,610,650,675,16),new Lane(970,615,970,675,16),
        new Lane(345,434,180,480,18),new Lane(180,480,4,550,18),
        new Lane(345,434,350,350,17),new Lane(350,350,350,300,17),new Lane(350,300,320,240,17),
        new Lane(320,240,255,215,17),new Lane(255,215,190,210,17),new Lane(190,210,145,180,17),
        new Lane(145,180,155,160,16),new Lane(255,215,420,195,15),new Lane(420,195,500,160,15),
        new Lane(500,160,600,165,15),new Lane(600,165,720,160,15),new Lane(720,160,780,180,16),
        new Lane(780,180,760,110,17),new Lane(760,110,745,4,17),
        new Lane(780,180,880,180,16),new Lane(880,180,1010,180,15),new Lane(1010,180,1050,160,15),
        new Lane(1050,160,1115,190,13),new Lane(1115,190,1130,250,13),new Lane(1130,250,1090,320,13),new Lane(1090,320,1075,390,13),new Lane(1075,390,1060,434,13),
        new Lane(180,480,140,430,15),new Lane(140,430,70,430,18),new Lane(180,480,235,430,16),new Lane(235,430,285,405,14),
        new Lane(350,434,355,520,17),new Lane(355,520,350,580,17),new Lane(350,580,395,650,17),
        new Lane(395,650,430,700,17),new Lane(430,700,415,760,17),new Lane(415,760,380,820,17),
        new Lane(380,820,405,900,17),new Lane(405,900,450,960,18),new Lane(450,960,590,1000,18),new Lane(590,1000,745,1000,17),
        new Lane(180,480,230,520,16),new Lane(230,520,250,570,16),new Lane(250,570,250,640,16),
        new Lane(250,640,215,700,16),new Lane(215,700,170,760,16),new Lane(170,760,150,800,16),
        new Lane(150,800,195,850,16),new Lane(195,850,280,890,16),new Lane(280,890,405,920,16),new Lane(405,920,450,960,16),
        new Lane(430,700,300,800,16),new Lane(300,800,380,820,16),
        new Lane(780,810,790,860,18),new Lane(790,860,790,980,11),new Lane(790,980,745,1000,17),new Lane(745,1000,760,1020,17),
        new Lane(790,980,900,985,17),new Lane(900,985,980,1020,17),
        new Lane(1060,675,1090,675,14),new Lane(1090,675,1090,810,9),new Lane(1090,810,1100,810,13),new Lane(1100,810,1185,810,16),
        new Lane(1185,810,1220,820,16),new Lane(1220,820,1220,940,16),new Lane(1220,940,1330,980,16),new Lane(1330,980,1415,1015,16),
        new Lane(1300,434,1325,405,12),new Lane(1325,405,1325,210,10),new Lane(1325,210,1380,185,12),
        new Lane(1300,434,1330,460,13),new Lane(1330,460,1350,520,13),new Lane(1350,520,1340,570,13),new Lane(1340,570,1360,645,13)
    };
    private static readonly Rect[] roofs={
        new Rect(72,20,220,140),new Rect(300,0,120,90),new Rect(515,60,130,75),new Rect(575,0,120,80),
        new Rect(830,0,140,75),new Rect(800,70,100,85),new Rect(970,55,140,85),new Rect(1290,20,180,150),
        new Rect(1340,225,160,115),new Rect(1370,320,159,120),new Rect(1390,485,146,200),
        new Rect(0,235,175,165),new Rect(220,315,112,85),
        new Rect(400,225,110,90),new Rect(580,245,145,115),new Rect(815,195,105,95),
        new Rect(990,200,120,165),new Rect(555,485,180,135),new Rect(850,500,190,125),
        new Rect(1100,690,170,100),new Rect(155,535,75,100),new Rect(257,675,130,110),
        new Rect(20,800,125,105),new Rect(230,910,150,110),new Rect(0,565,100,100),
        new Rect(470,720,260,160),new Rect(820,720,250,160),new Rect(1030,850,180,125),new Rect(1250,780,135,110),
        new Rect(620,895,70,65),new Rect(700,900,70,50),new Rect(812,900,75,50)
    };
    public static bool Walkable(Vector2 point)
    {
        foreach(var roof in roofs)
            if(new Rect(roof.x-1,roof.y-1,roof.width+2,roof.height+2).Contains(point))return false;
        if((point-new Vector2(750,875)).sqrMagnitude<18*18)return false;
        if(new Rect(724,354,199,142).Contains(point))return true;
        foreach(var lane in lanes)
        {
            Vector2 segment=lane.b-lane.a;
            float t=Mathf.Clamp01(Vector2.Dot(point-lane.a,segment)/segment.sqrMagnitude);
            if((point-(lane.a+segment*t)).sqrMagnitude<=lane.radius*lane.radius)return true;
        }
        return false;
    }
}
