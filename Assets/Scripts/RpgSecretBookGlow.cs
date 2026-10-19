using UnityEngine;

// The shelf clue only lights up when the player is close enough to discover it.
public sealed class RpgSecretBookGlow : MonoBehaviour
{
    public const float BookReach=1.15f;
    private RpgGame game;
    private SpriteRenderer art,halo;
    private TextMesh label;
    private bool proximityOnly;
    private static Sprite glow;
    public void Initialize(RpgGame owner,SpriteRenderer image,TextMesh caption,bool nearby)
    {
        game=owner;art=image;label=caption;proximityOnly=nearby;
        var obj=new GameObject("Jemná záře");obj.transform.SetParent(transform,false);
        halo=obj.AddComponent<SpriteRenderer>();halo.sprite=Glow();halo.sortingOrder=image.sortingOrder-1;
        if(label!=null)label.gameObject.SetActive(false);
        halo.enabled=!nearby;
    }
    private void Update()
    {
        if(game==null||art==null)return;
        bool close=Vector2.Distance(game.Player.position,transform.position)<=(proximityOnly?BookReach:1.8f);
        bool visible=!proximityOnly||close;
        halo.enabled=visible;
        if(label!=null)label.gameObject.SetActive(close&&!game.BlockMovement);
        float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*3);
        halo.color=new Color(1,.75f,.25f,visible?.4f+pulse*.22f:0);
        halo.transform.localScale=Vector3.one*(1+pulse*.15f);
        art.color=Color.white;
    }
    private static Sprite Glow()
    {
        if(glow!=null)return glow;
        const int n=32;var pixels=new Color32[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {
            float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16))/16;
            pixels[y*n+x]=new Color32(255,227,142,(byte)(Mathf.Clamp01(1-distance)*140));
        }
        var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};texture.SetPixels32(pixels);texture.Apply(false,true);
        glow=Sprite.Create(texture,new Rect(0,0,n,n),Vector2.one*.5f,18);return glow;
    }
}
