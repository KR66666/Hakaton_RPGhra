using System.Collections.Generic;
using UnityEngine;

// Use the imported sprite silhouette to remove transparent margins without
// changing the artwork or requiring readable texture data at runtime.
public static class RpgPortraitFit
{
    private static readonly Dictionary<string,Sprite> originalPortraits=new Dictionary<string,Sprite>();
    public static Sprite Blacksmith() => OriginalWhitePortrait("BlacksmithPortraitOriginal");
    public static Sprite Librarian() => Resources.Load<Sprite>("LibrarianPortrait");
    public static Sprite HenriShop() => Resources.Load<Sprite>("HenriPortrait");
    private static Sprite OriginalWhitePortrait(string resource)
    {
        if(originalPortraits.TryGetValue(resource,out var cached)&&cached!=null)return cached;
        var original=Resources.Load<Texture2D>(resource);if(original==null)return null;
        var pixels=original.GetPixels32();
        // Remove the edge-connected white JPG backdrop and its grey fringe, preserving eye highlights.
        int width=original.width,height=original.height;
        var seen=new bool[pixels.Length];var queue=new Queue<int>();
        System.Action<int> seed=i=>
        {
            if(seen[i])return;seen[i]=true;
            var c=pixels[i];int min=Mathf.Min(c.r,c.g,c.b),max=Mathf.Max(c.r,c.g,c.b);
            if(c.a>0&&(min<190||max-min>30))return;
            pixels[i].a=0;queue.Enqueue(i);
        };
        for(int x=0;x<width;x++){seed(x);seed((height-1)*width+x);}
        for(int y=0;y<height;y++){seed(y*width);seed(y*width+width-1);}
        while(queue.Count>0)
        {
            int i=queue.Dequeue(),x=i%width,y=i/width;
            if(x>0)seed(i-1);if(x<width-1)seed(i+1);if(y>0)seed(i-width);if(y<height-1)seed(i+width);
        }
        var texture=new Texture2D(original.width,original.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels32(pixels);texture.Apply(false,true);
        var portrait=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100);
        originalPortraits[resource]=portrait;return portrait;
    }
    public static Sprite ArabellaShop() => Resources.Load<Sprite>("ArabellaShopPortraitCutout") ?? Resources.Load<Sprite>("ArabellaPortrait");
    private static readonly Dictionary<Sprite,Sprite> cache=new Dictionary<Sprite,Sprite>();
    public static Sprite VisibleSprite(Sprite source)
    {
        if(source==null||source.packed)return source;
        if(cache.TryGetValue(source,out var cached)&&cached!=null)return cached;
        var vertices=source.vertices;
        if(vertices.Length==0)return source;
        Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        foreach(var vertex in vertices)
        {
            Vector2 point=vertex*source.pixelsPerUnit+source.pivot;
            min=Vector2.Min(min,point);max=Vector2.Max(max,point);
        }
        var original=source.rect;
        min=new Vector2(Mathf.Clamp(Mathf.Floor(min.x),0,original.width),Mathf.Clamp(Mathf.Floor(min.y),0,original.height));
        max=new Vector2(Mathf.Clamp(Mathf.Ceil(max.x),0,original.width),Mathf.Clamp(Mathf.Ceil(max.y),0,original.height));
        if(max.x-min.x<1||max.y-min.y<1)return source;
        var rect=new Rect(original.x+min.x,original.y+min.y,max.x-min.x,max.y-min.y);
        var fitted=Sprite.Create(source.texture,rect,new Vector2(.5f,0),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        fitted.name=source.name+" • rámovaný portrét";cache[source]=fitted;return fitted;
    }
}
