using System.Collections.Generic;
using UnityEngine;

// Exact supplied JPG pixels are cut into frames. Only the outer white backdrop is removed.
public static class RpgDirectionalHero
{
    private static Sprite[,] frames;
    public static bool Available {get {Ensure();return frames!=null;}}
    public static Sprite Frame(int direction,int step)
    {
        Ensure();if(frames==null)return null;
        int view=direction==1?1:direction>=2?2:0;
        int pose=step<=0?(view==2?2:0):step%4;
        // Alternate open and closed strides in profile so both feet visibly take turns.
        if(view==2 && step>0){int[] sideCycle={0,2,1,3};pose=sideCycle[(step-1)%4];}
        return frames[view,pose];
    }
    private static void Ensure()
    {
        if(frames!=null)return;
        string[] names={"Front","Back","Left"};var sources=new Texture2D[3];
        for(int i=0;i<3;i++){sources[i]=Resources.Load<Texture2D>("HeroDirectional/"+names[i]);if(sources[i]==null)return;}
        frames=new Sprite[3,4];
        for(int view=0;view<3;view++)
        {
            var source=sources[view];int width=source.width,fullHeight=source.height,height=Mathf.RoundToInt(fullHeight*.84f);
            var original=source.GetPixels32();var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)System.Array.Copy(original,(y+fullHeight-height)*width,pixels,y*width,width);
            var visited=new bool[pixels.Length];var queue=new Queue<int>();
            System.Action<int> seed=index=>{
                if(visited[index])return;visited[index]=true;
                var c=pixels[index];if(Mathf.Min(c.r,c.g,c.b)<228 || Mathf.Max(c.r,c.g,c.b)-Mathf.Min(c.r,c.g,c.b)>22)return;
                pixels[index].a=0;queue.Enqueue(index);
            };
            for(int x=0;x<width;x++){seed(x);seed((height-1)*width+x);}
            for(int y=0;y<height;y++){seed(y*width);seed(y*width+width-1);}
            while(queue.Count>0)
            {
                int index=queue.Dequeue(),x=index%width,y=index/width;
                if(x>0)seed(index-1);if(x<width-1)seed(index+1);if(y>0)seed(index-width);if(y<height-1)seed(index+width);
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Dodaná hrdinka • "+names[view],filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            for(int pose=0;pose<4;pose++)
            {
                int left=Mathf.RoundToInt(width*pose/4f),right=Mathf.RoundToInt(width*(pose+1)/4f);
                var sprite=Sprite.Create(texture,new Rect(left,0,right-left,height),Vector2.one*.5f,height/1.4f);
                sprite.name="Heroine "+names[view]+" "+pose;frames[view,pose]=sprite;
            }
        }
    }
}
