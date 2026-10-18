using System.Collections.Generic;
using UnityEngine;

// Small native pixel sprites, viewed above the shoulders; independent of player artwork.
public static class MedievalNpcAppearance
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    public static Sprite Get(string role, int variant = 0)
    {
        variant = Mathf.Abs(variant) % 4;
        string key = role + variant;
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        const int n = 48;
        var pixels = new Color32[n*n];
        Color32 skin = C(185 + variant*8,125 + variant*5,85), leather=C(89,57,34), boot=C(39,31,27);
        Color32 cloth = role == "mayor" ? C(121,52,48) : role == "merchant" ? C(143,99,44) :
            role == "healer" ? C(62,100,66) : role == "guard" ? C(70,89,125) : role == "boss" ? C(107,43,48) : C(79+variant*7,68,52);
        Color32 hair=C(65+variant*15,41+variant*10,27+variant*5), steel=C(124,137,139), gold=C(198,159,74);
        System.Action<int,int,int,int,Color32> box=(x,y,w,h,c)=>
        { for(int yy=y;yy<y+h;yy++) for(int xx=x;xx<x+w;xx++) if(xx>=0&&yy>=0&&xx<n&&yy<n) pixels[yy*n+xx]=c; };
        System.Action<int,int,int,int,Color32> oval=(cx,cy,rx,ry,c)=>
        { for(int y=cy-ry;y<=cy+ry;y++) for(int x=cx-rx;x<=cx+rx;x++)
            if(x>=0&&y>=0&&x<n&&y<n&&(x-cx)*(x-cx)/(float)(rx*rx)+(y-cy)*(y-cy)/(float)(ry*ry)<=1) pixels[y*n+x]=c; };
        box(17,6,6,12,leather);box(26,7,6,11,leather);
        box(16,4,7,6,boot);box(26,5,7,6,boot);
        oval(24,23,16,11,boot);oval(24,24,15,10,cloth);
        box(10,18,5,7,cloth);box(34,18,5,7,cloth);
        box(10,16,4,5,skin);box(35,16,4,5,skin);
        box(16,17,17,3,leather);box(23,17,3,3,gold);
        // Cloth weave and folds keep the pixel silhouettes readable at game scale.
        for(int y=21;y<30;y+=3) { box(15,y,3,1,Shade(cloth,20));box(30,y,3,1,Shade(cloth,-18)); }
        oval(24,29,9,9,boot);oval(24,30,8,8,hair);oval(21,33,4,4,Shade(hair,26));
        box(24,29,1,9,Shade(hair,-20));
        if(role=="mayor")
        { oval(24,34,10,5,C(94,43,43));box(16,35,16,3,C(142,61,54));box(30,36,2,6,gold);box(17,25,2,5,gold); }
        else if(role=="merchant")
        { box(17,19,14,6,C(179,155,112));box(33,12,7,10,leather);box(35,15,3,2,gold);oval(24,36,9,3,C(188,165,122)); }
        else if(role=="healer")
        { oval(24,31,11,10,C(118,136,95));oval(24,32,7,7,C(178,183,137));box(35,13,6,9,C(90,61,36));box(36,21,2,5,C(83,135,57));box(39,20,2,5,C(111,153,69)); }
        else if(role=="guard" || role=="boss")
        { oval(24,31,10,10,C(63,73,79));oval(24,32,9,9,steel);box(23,24,3,16,C(174,183,181));box(17,36,3,2,C(192,199,189));
          box(6,11,2,29,leather);box(5,37,4,6,steel);box(34,17,9,13,role=="boss"?C(112,40,42):C(50,72,114));box(38,18,2,11,gold);
          if(role=="boss") box(22,40,5,5,C(164,51,50)); }
        else
        { oval(24,32,11,10,leather);oval(24,33,8,8,C(75,57,37));box(34,14,3,15,steel);box(32,17,7,2,leather); }
        var texture=new Texture2D(n,n,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp, name="NPC "+key };
        texture.SetPixels32(pixels);texture.Apply(false,true);
        var sprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),36,0,SpriteMeshType.FullRect);
        sprite.name="Medieval "+key;cache[key]=sprite;return sprite;
    }
    private static Color32 C(int r,int g,int b) => new Color32((byte)Mathf.Clamp(r,0,255),(byte)Mathf.Clamp(g,0,255),(byte)Mathf.Clamp(b,0,255),255);
    private static Color32 Shade(Color32 c,int amount) => C(c.r+amount,c.g+amount,c.b+amount);
}
