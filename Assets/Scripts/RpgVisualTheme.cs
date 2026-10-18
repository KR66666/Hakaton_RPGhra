using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One palette and native pixel UI shared by the village, portraits, menus and pickups.
public static class RpgVisualTheme
{
    public static readonly Color Cream=new Color32(238,222,190,255);
    public static readonly Color Gold=new Color32(190,158,98,255);
    public static readonly Color Dark=new Color32(42,31,24,255);
    private static readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
    private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    private static Sprite panel;
    private static Sprite parchment;
    public static Sprite Parchment
    {
        get
        {
            if(parchment!=null)return parchment;
            var pixels=new Color32[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                int d=Mathf.Min(x,y,31-x,31-y),grain=(x*13+y*7)%5;
                pixels[y*32+x]=d==0?new Color32(67,43,28,255):d<3?new Color32(149,113,65,255):
                    new Color32((byte)(211+grain),(byte)(189+grain),(byte)(147+grain),255);
            }
            parchment=Make(pixels,32,"Pergamen",new Vector4(5,5,5,5));return parchment;
        }
    }
    public static Material Art(bool terrain=false,int block=2)
    {
        string key=terrain+":"+block;
        if(materials.TryGetValue(key,out var cached)&&cached!=null)return cached;
        var shader=Resources.Load<Shader>("Shaders/BrightPixelArt");
        if(shader==null)return null;
        var material=new Material(shader){name="Světlá pixelová paleta • "+key};
        material.SetFloat("_BlockSize",block);material.SetFloat("_Terrain",terrain?1:0);
        materials[key]=material;return material;
    }
    public static Material FantasyAtmosphere()
    {
        const string key="FantasyAtmosphere";
        if(materials.TryGetValue(key,out var cached)&&cached!=null)return cached;
        var material=new Material(Resources.Load<Shader>("Shaders/BrightPixelArt")){name="Fantasy • jemné přirozené světlo"};
        material.SetFloat("_Natural",1);materials[key]=material;return material;
    }
    public static void World(SpriteRenderer renderer,bool terrain=false,int block=1)
    {
        if(renderer==null)return;
        renderer.sharedMaterial=Art(terrain,block);
        if(renderer.sprite!=null)renderer.sprite.texture.filterMode=FilterMode.Point;
    }
    public static void Picture(Image image,bool terrain=false,int block=3)
    {
        if(image==null)return;image.material=Art(terrain,block);
        if(image.sprite!=null)image.sprite.texture.filterMode=FilterMode.Point;
    }
    public static Sprite Frame
    {
        get
        {
            if(panel!=null)return panel;
            var colors=new Color32[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                int d=Mathf.Min(x,y,31-x,31-y);
                bool rivet=(x<5||x>26)&&(y<5||y>26);
                colors[y*32+x]=d==0?new Color32(24,18,14,255):d==1?new Color32(113,88,52,255):
                    d==2?new Color32(70,47,29,255):d==3?new Color32(92,63,38,255):
                    rivet?new Color32(153,127,78,255):d==4?new Color32(24,18,14,255):
                    (x*3+y)%11==0?new Color32(48,35,26,255):new Color32(39,29,23,255);
            }
            panel=Make(colors,32,"Pixelový rám",new Vector4(6,6,6,6));return panel;
        }
    }
    public static void Panel(Image image)
    {
        image.sprite=Frame;image.type=Image.Type.Sliced;image.color=Color.white;
    }
    private static Sprite Make(Color32[] pixels,int size,string name,Vector4 border=default)
    {
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=name};
        texture.SetPixels32(pixels);texture.Apply(false,true);
        var sprite=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,border.x>0?100:16,0,SpriteMeshType.FullRect,border);sprite.name=name;return sprite;
    }
    public static Sprite Icon(string kind)
    {
        if(icons.TryGetValue(kind,out var cached)&&cached!=null)return cached;
        const int n=24;var pixels=new Color32[n*n];
        Color32 ink=new Color32(39,39,39,255),gold=new Color32(246,193,80,255),light=new Color32(255,240,176,255),wood=new Color32(155,83,44,255),green=new Color32(75,171,58,255),steel=new Color32(166,220,218,255);
        System.Action<int,int,int,int,Color32> box=(x,y,w,h,c)=>{for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)if(xx>=0&&yy>=0&&xx<n&&yy<n)pixels[yy*n+xx]=c;};
        if(kind=="heart")
        {
            string[] rows={"001110011100","011111111110","111111111111","111111111111","011111111110","001111111100","000111111000","000011110000","000001100000"};
            for(int y=0;y<rows.Length;y++)for(int x=0;x<12;x++)if(rows[y][x]=='1')box(2+x,16-y,1,1,new Color32(35,42,42,255));
            for(int y=1;y<rows.Length-1;y++)for(int x=1;x<11;x++)if(rows[y][x]=='1'&&rows[y-1][x]=='1'&&rows[y+1][x]=='1'&&rows[y][x-1]=='1'&&rows[y][x+1]=='1')box(2+x,16-y,1,1,new Color32(47,196,153,255));
            box(5,14,2,1,light);
        }
        else if(kind.Contains("weapon")||kind=="sword")
        {for(int i=0;i<13;i++){box(5+i,5+i,4,4,ink);box(6+i,6+i,2,2,steel);}box(4,2,3,6,wood);box(2,7,9,3,gold);box(18,19,2,2,light);}
        else if(kind.Contains("armor"))
        {box(5,4,14,16,ink);box(2,12,20,6,ink);box(6,5,12,14,steel);box(3,13,18,4,steel);box(10,16,4,5,ink);box(7,5,3,12,light);box(6,6,12,2,gold);}
        else if(kind.Contains("potion"))
        {box(8,16,8,5,ink);box(9,17,6,3,wood);box(7,4,10,13,ink);box(5,6,14,8,ink);box(7,6,10,8,new Color32(121,90,202,255));box(9,14,6,3,steel);box(8,11,2,3,light);}
        else if(kind=="herb"||kind=="garden")
        {box(11,3,2,15,ink);box(12,3,1,14,green);box(5,7,7,5,ink);box(6,8,5,3,green);box(13,12,7,5,ink);box(14,13,5,3,green);box(9,17,6,4,green);box(10,18,2,2,light);}
        else if(kind=="testgear")
        {box(7,3,10,18,ink);box(8,4,8,16,light);for(int y=7;y<18;y+=3)box(10,y,4,1,wood);box(6,18,12,3,gold);}
        else
        {box(3,4,18,15,ink);box(4,5,16,13,wood);box(4,11,16,2,ink);box(5,14,14,3,gold);box(6,5,2,13,gold);box(16,5,2,13,gold);box(10,9,4,5,ink);box(11,10,2,3,light);}
        icons[kind]=Make(pixels,n,"Ikona • "+kind);return icons[kind];
    }
}
