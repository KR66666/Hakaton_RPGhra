using System.Collections.Generic;
using UnityEngine;

// Exact supplied JPG pixels are cut into frames. Only the outer white backdrop is removed.
public static class RpgDirectionalHero
{
    private static readonly Sprite[][,] heroFrames=new Sprite[5][,];
    public static bool Available => IsAvailable(false);
    public static bool IsAvailable(bool boy) {Ensure(boy?1:0);return heroFrames[boy?1:0]!=null;}
    public static Sprite Frame(int direction,int step,bool boy=false) => CharacterFrame(direction,step,boy?1:0);
    public static Sprite MatejFrame(int direction,int step) => CharacterFrame(direction,step,2);
    public static Sprite PetrFrame(int direction,int step) => CharacterFrame(direction,step,3);
    public static Sprite BlacksmithFrame(int direction,int step) => CharacterFrame(direction,step,4);
    private static Sprite CharacterFrame(int direction,int step,int character)
    {
        Ensure(character);var frames=heroFrames[character];if(frames==null)return null;
        int view=direction==1?1:direction>=2?2:0;
        int pose=step<=0?(view==2&&character!=3?2:0):step%4;
        // Alternate open and closed strides in profile so both feet visibly take turns.
        if(view==2 && step>0){int[] sideCycle={0,2,1,3};pose=sideCycle[(step-1)%4];}
        return frames[view,pose];
    }
    private static void Ensure(int character)
    {
        if(heroFrames[character]!=null)return;
        bool boy=character==1;
        string[] names={"Front","Back","Left"};var sources=new Texture2D[3];
        string folder=character==4?"BlacksmithDirectional/":character==3?"PetrDirectional/":character==2?"MatejDirectional/":boy?"HeroBoyDirectional/":"HeroDirectional/";
        for(int i=0;i<3;i++){sources[i]=Resources.Load<Texture2D>(folder+names[i]);if(sources[i]==null)return;}
        var frames=new Sprite[3,4];heroFrames[character]=frames;
        for(int view=0;view<3;view++)
        {
            var source=sources[view];int width=source.width,fullHeight=source.height,height=Mathf.RoundToInt(fullHeight*(character==3?(view==2?.85f:.89f):.84f));
            if(character==4)height=Mathf.RoundToInt(fullHeight*(view==1?.58f:view==2?.9f:.84f));
            int sourceY=character==4&&view==1?Mathf.RoundToInt(fullHeight*.21f):fullHeight-height;
            var original=source.GetPixels32();var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)System.Array.Copy(original,(y+sourceY)*width,pixels,y*width,width);
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
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name=(boy?"Dodaný hrdina • ":"Dodaná hrdinka • ")+names[view],filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            for(int pose=0;pose<4;pose++)
            {
                int left=Mathf.RoundToInt(width*pose/4f),right=Mathf.RoundToInt(width*(pose+1)/4f);
                // Petr's raised sword extends above his head; match his body to the heroes.
                var sprite=Sprite.Create(texture,new Rect(left,0,right-left,height),Vector2.one*.5f,height/(character==3?1.7f:1.4f));
                sprite.name=(boy?"Hero ":"Heroine ")+names[view]+" "+pose;frames[view,pose]=sprite;
            }
        }
    }
}
