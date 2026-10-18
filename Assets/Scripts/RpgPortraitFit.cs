using System.Collections.Generic;
using UnityEngine;

// Use the imported sprite silhouette to remove transparent margins without
// changing the artwork or requiring readable texture data at runtime.
public static class RpgPortraitFit
{
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
