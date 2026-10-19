using System.Collections.Generic;
using UnityEngine;

// Temporary top-down pixel maps with collision footprints shared with RpgLocations.
public static class RpgLocationArt
{
    private static readonly Dictionary<RpgPlace,Sprite> cache=new Dictionary<RpgPlace,Sprite>();
    public static Sprite Map(RpgPlace place)
    {
        if(cache.TryGetValue(place,out var sprite)&&sprite!=null)return sprite;
        if(place==RpgPlace.Forest || place==RpgPlace.DarkForest)
        {
            sprite=Resources.Load<Sprite>(place==RpgPlace.Forest?"ForestMap":"DarkForestMap");
            if(sprite!=null){cache[place]=sprite;return sprite;}
        }
        if(place==RpgPlace.Library||place==RpgPlace.SecretLibrary)
        {
            sprite=Resources.Load<Sprite>(place==RpgPlace.Library?"LibraryWalkableMap":"LibrarySecretRoom");
            if(sprite!=null){cache[place]=sprite;return sprite;}
        }
        const int w=384,h=288;
        var pixels=new Color32[w*h];
        bool forest=place==RpgPlace.Forest||place==RpgPlace.DarkForest,dark=place==RpgPlace.DarkForest,library=place==RpgPlace.Library;
        Color ground=forest?(dark?new Color(.07f,.12f,.12f):new Color(.19f,.28f,.17f)):library?new Color(.32f,.23f,.14f):new Color(.23f,.24f,.20f);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            int noise=(x*17+y*31+(x*y)%23)%13;
            Color color=ground*(.92f+noise*.012f);
            if(forest&&(Mathf.Abs(x-w/2)<19||Mathf.Abs(y-h/2)<14))color=dark?new Color(.16f,.17f,.15f):new Color(.37f,.33f,.21f);
            if(library&&(x%48==0||y%12==0))color*=.65f;
            if(!forest&&!library&&(x%24==0||y%18==0))color*=.7f;
            pixels[y*w+x]=color;
        }
        System.Action<int,int,int,int,Color> box=(x,y,ww,hh,c)=>{
            for(int yy=Mathf.Max(0,y);yy<Mathf.Min(h,y+hh);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(w,x+ww);xx++)pixels[yy*w+xx]=c;
        };
        System.Action<int,int,int,int,Color> oval=(cx,cy,rx,ry,c)=>{
            for(int y=Mathf.Max(0,cy-ry);y<=Mathf.Min(h-1,cy+ry);y++)for(int x=Mathf.Max(0,cx-rx);x<=Mathf.Min(w-1,cx+rx);x++)
                if((x-cx)*(x-cx)/(float)(rx*rx)+(y-cy)*(y-cy)/(float)(ry*ry)<=1)pixels[y*w+x]=c;
        };
        foreach(var rect in RpgLocations.Obstacles(place))
        {
            int x=Mathf.RoundToInt((rect.x+16)*12),y=Mathf.RoundToInt((rect.y+12)*12);
            int rw=Mathf.RoundToInt(rect.width*12),rh=Mathf.RoundToInt(rect.height*12);
            if(forest)
            {
                int cx=x+rw/2,cy=y+rh/2;
                oval(cx+3,cy-4,21,18,new Color(.04f,.07f,.06f));
                box(cx-3,cy-8,6,17,new Color(.22f,.15f,.09f));
                oval(cx,cy+6,18,20,dark?new Color(.09f,.18f,.17f):new Color(.16f,.34f,.19f));
                oval(cx-5,cy+12,12,13,dark?new Color(.12f,.23f,.21f):new Color(.23f,.42f,.22f));
            }
            else if(library)
            {
                box(x-2,y-3,rw+4,rh+5,new Color(.11f,.08f,.06f));box(x,y,rw,rh,new Color(.45f,.29f,.14f));
                for(int bx=x+3;bx<x+rw-3;bx+=5)box(bx,y+3,3,rh-6,new Color(.25f+(bx%7)*.07f,.18f+(bx%5)*.06f,.15f+(bx%3)*.09f));
            }
            else
            {
                box(x-2,y-2,rw+4,rh+4,new Color(.12f,.13f,.12f));box(x,y,rw,rh,new Color(.47f,.46f,.39f));
                for(int xx=x;xx<x+rw;xx+=12)box(xx,y,1,rh,new Color(.29f,.30f,.25f));
                box(x+2,y+rh-3,rw-4,2,new Color(.59f,.58f,.48f));
            }
        }
        // Enclosing edges and an open southern doorway make the indoor map legible.
        if(library){box(0,0,165,7,new Color(.12f,.09f,.06f));box(219,0,w-219,7,new Color(.12f,.09f,.06f));box(0,h-7,w,7,new Color(.12f,.09f,.06f));box(0,0,7,h,new Color(.12f,.09f,.06f));box(w-7,0,7,h,new Color(.12f,.09f,.06f));}
        var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=RpgLocations.Name(place)};
        texture.SetPixels32(pixels);texture.Apply(false,true);
        sprite=Sprite.Create(texture,new Rect(0,0,w,h),Vector2.one*.5f,12);sprite.name=RpgLocations.Name(place);
        cache[place]=sprite;return sprite;
    }
}
