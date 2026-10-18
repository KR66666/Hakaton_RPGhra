using System.Collections;
using UnityEngine;

public sealed partial class RpgGame
{
    private static Sprite secretCandleArt,secretDoorArt,secretBookArt,secretShelfArt;
    private void BuildLibrarySecret()
    {
        if(State.place==RpgPlace.SecretLibrary)
        {
            var shelf=new GameObject("Malá polička pro jedinou svíčku");shelf.transform.position=new Vector3(-9.5f,1.2f,0)*RpgLocations.SecretRoomScale;
            shelf.transform.localScale=Vector3.one*RpgLocations.SecretRoomScale;
            var shelfRenderer=shelf.AddComponent<SpriteRenderer>();shelfRenderer.sprite=SecretShelfArt();shelfRenderer.sortingOrder=2;worldObjects.Add(shelf);
            if(!State.libraryCandleTaken)
            {
                var candle=new GameObject("Jediná svíčka tajného pokoje");candle.transform.position=new Vector3(-9.5f,1.65f,0)*RpgLocations.SecretRoomScale;
                candle.transform.localScale=Vector3.one*.9f;
                var art=candle.AddComponent<SpriteRenderer>();art.sprite=SecretArt(false);art.sortingOrder=3;
                candle.AddComponent<RpgSecretBookGlow>().Initialize(this,art,null,false);
                AddLabel(candle.transform,"[E] Vzít svíčku",new Vector3(0,-.8f,0),new Color(1,.86f,.57f));
                worldItems.Add(new WorldItem{id="archiveCandle",kind="archiveCandle",title="Vzít svíčku z tajného pokoje",obj=candle});worldObjects.Add(candle);
            }
            return;
        }
        if(State.place!=RpgPlace.Library)return;
        if(State.librarySecretOpened)
        {
            var door=new GameObject("Otevřený tajný průchod");
            door.transform.position=new Vector3(11.4f,4.7f,0);
            var art=door.AddComponent<SpriteRenderer>();art.sprite=SecretArt(true);art.sortingOrder=1;
            worldObjects.Add(door);return;
        }
        var book=new GameObject("Kniha se skrytým mechanismem");
        book.transform.position=new Vector3(11.4f,5.1f,0);
        // The visible book is already painted in the shelf: don't overlay different artwork.
        var renderer=book.AddComponent<SpriteRenderer>();renderer.sortingOrder=3;
        var label=AddLabel(book.transform,"[E] Vytáhnout knihu",new Vector3(0,-.8f,0),new Color(1,.86f,.57f));
        book.AddComponent<RpgSecretBookGlow>().Initialize(this,renderer,label,true);
        worldItems.Add(new WorldItem{id="libraryBook",kind="secretBook",title="Vytáhnout zářící knihu",obj=book});
        worldObjects.Add(book);
    }
    private static Sprite SecretShelfArt()
    {
        if(secretShelfArt!=null)return secretShelfArt;
        const int w=36,h=16;var pixels=new Color32[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            if(y>=7&&y<=14)
            {
                int grain=(x*7+y*13)%17;
                pixels[y*w+x]=y==14?new Color32(171,119,63,255):y==7?new Color32(63,37,24,255):new Color32((byte)(105+grain),(byte)(65+grain/2),34,255);
            }
            else if(y<7&&(x>=4&&x<=7||x>=28&&x<=31))pixels[y*w+x]=new Color32(67,40,24,255);
        }
        var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};texture.SetPixels32(pixels);texture.Apply(false,true);
        secretShelfArt=Sprite.Create(texture,new Rect(0,0,w,h),Vector2.one*.5f,22);return secretShelfArt;
    }
    private void PullLibraryBook(GameObject book)
    {
        if(State.place!=RpgPlace.Library||State.librarySecretOpened||IsEnteringRoom)return;
        if(Vector2.Distance(transform.position,book.transform.position)>RpgSecretBookGlow.BookReach)return;
        StartCoroutine(OpenLibrarySecret(book));
    }
    private IEnumerator OpenLibrarySecret(GameObject candle)
    {
        IsEnteringRoom=true;
        Notify("Vytahuješ knihu… za regálem zacvakl mechanismus.",5);
        float elapsed=0;
        while(elapsed<.65f&&candle!=null)
        {
            elapsed+=Time.unscaledDeltaTime;
            candle.transform.rotation=Quaternion.Euler(0,0,Mathf.Lerp(0,-35,Mathf.Clamp01(elapsed/.65f)));
            yield return null;
        }
        State.librarySecretOpened=true;
        BuildWorld();IsEnteringRoom=false;Save(false);
        Notify("Otevřel se tajný průchod! Přijdi k němu a stiskni E.",7);
    }
    private static Sprite SecretBookArt()
    {
        if(secretBookArt!=null)return secretBookArt;
        const int width=12,height=20;var pixels=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            pixels[y*width+x]=x<2||x>9||y<2||y>17?new Color32(67,39,25,255):y==4||y==15?new Color32(197,151,67,255):new Color32(43,91,114,255);
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};texture.SetPixels32(pixels);texture.Apply(false,true);
        secretBookArt=Sprite.Create(texture,new Rect(0,0,width,height),Vector2.one*.5f,28);return secretBookArt;
    }
    private static Sprite SecretArt(bool door)
    {
        if(door&&secretDoorArt!=null)return secretDoorArt;
        if(!door&&secretCandleArt!=null)return secretCandleArt;
        int width=door?32:12,height=door?44:28;
        var pixels=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            if(door)
            {
                int edge=Mathf.Min(x,width-1-x);
                pixels[y*width+x]=edge<3||y>height-5?new Color32((byte)(85+(y/5%2)*15),66,43,255):new Color32(19,17,18,255);
            }
            else if(y<5&&x>0&&x<11)pixels[y*width+x]=new Color32(120,85,39,255);
            else if(y>=5&&y<21&&x>=4&&x<=8)pixels[y*width+x]=new Color32(229,202,146,255);
            else if(y>=22&&y<27&&x>=4&&x<=7)pixels[y*width+x]=new Color32(255,181,55,255);
        }
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels32(pixels);texture.Apply(false,true);
        var sprite=Sprite.Create(texture,new Rect(0,0,width,height),Vector2.one*.5f,door?22:28);
        if(door)secretDoorArt=sprite;else secretCandleArt=sprite;
        return sprite;
    }
}
