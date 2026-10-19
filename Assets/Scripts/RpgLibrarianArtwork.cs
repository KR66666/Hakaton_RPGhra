using System.Collections.Generic;
using UnityEngine;

// Cut the supplied sheet without redrawing the librarian or including the frame numbers.
public static class RpgLibrarianArtwork
{
    private static Sprite[,] walk;
    private static Sprite[] idle;
    public static Sprite Frame(int direction,int step)
    {
        Ensure();if(walk==null)return null;
        int view=direction==1?2:direction>=2?1:0;
        return step<0?idle[view]:walk[view,step%4];
    }
    private static void Ensure()
    {
        if(walk!=null)return;
        var source=Resources.Load<Texture2D>("LibrarianSheet");if(source==null)return;
        var pixels=source.GetPixels32();walk=new Sprite[3,4];idle=new Sprite[3];
        float[][] xs={new float[]{4,140,275,410},new float[]{582,723,863,1003},new float[]{1216,1352,1489,1625}};
        for(int view=0;view<3;view++)for(int pose=0;pose<4;pose++)
            walk[view,pose]=Cut(source,pixels,new Rect(xs[view][pose],522,view==1?143:139,281));
        idle[0]=Cut(source,pixels,new Rect(270,12,242,488));
        idle[1]=Cut(source,pixels,new Rect(940,12,245,488));
        idle[2]=Cut(source,pixels,new Rect(1260,12,245,488));
    }
    private static Sprite Cut(Texture2D source,Color32[] original,Rect reference)
    {
        int x=Mathf.RoundToInt(reference.x/1774*source.width),top=Mathf.RoundToInt(reference.y/887*source.height);
        int width=Mathf.RoundToInt(reference.width/1774*source.width),height=Mathf.RoundToInt(reference.height/887*source.height);
        x=Mathf.Clamp(x,0,source.width-1);width=Mathf.Min(width,source.width-x);height=Mathf.Min(height,source.height-top);
        int bottom=source.height-top-height;var pixels=new Color32[width*height];
        for(int y=0;y<height;y++)System.Array.Copy(original,(bottom+y)*source.width+x,pixels,y*width,width);
        var seen=new bool[pixels.Length];var queue=new Queue<int>();
        System.Action<int> seed=i=>{
            if(seen[i])return;seen[i]=true;var c=pixels[i];
            if(c.a>0&&(Mathf.Min(c.r,c.g,c.b)<230||Mathf.Max(c.r,c.g,c.b)-Mathf.Min(c.r,c.g,c.b)>22))return;
            pixels[i].a=0;queue.Enqueue(i);
        };
        for(int xx=0;xx<width;xx++){seed(xx);seed((height-1)*width+xx);}
        for(int yy=0;yy<height;yy++){seed(yy*width);seed(yy*width+width-1);}
        while(queue.Count>0)
        {
            int i=queue.Dequeue(),xx=i%width,yy=i/width;
            if(xx>0)seed(i-1);if(xx<width-1)seed(i+1);if(yy>0)seed(i-width);if(yy<height-1)seed(i+width);
        }
        int left=width-1,right=0,low=height-1,high=0;
        for(int yy=0;yy<height;yy++)for(int xx=0;xx<width;xx++)if(pixels[yy*width+xx].a>0)
        {left=Mathf.Min(left,xx);right=Mathf.Max(right,xx);low=Mathf.Min(low,yy);high=Mathf.Max(high,yy);}
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels32(pixels);texture.Apply(false,true);
        return Sprite.Create(texture,new Rect(left,low,right-left+1,high-low+1),Vector2.one*.5f,(high-low+1)/1.4f,0,SpriteMeshType.FullRect);
    }
}
