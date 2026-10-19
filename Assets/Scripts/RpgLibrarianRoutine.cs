using UnityEngine;

public sealed class RpgLibrarianRoutine : MonoBehaviour
{
    private RpgGame game;
    private SpriteRenderer art;
    private Vector3[] route;
    private int next,direction;
    private float waitUntil,distance,stalled;
    public static readonly Vector2[] RoutePixels={
        new Vector2(768,860),new Vector2(768,700),new Vector2(428,700),new Vector2(428,515),
        new Vector2(768,515),new Vector2(1110,515),new Vector2(1110,350),new Vector2(768,350),
        new Vector2(768,515),new Vector2(768,860)};
    public void Initialize(RpgGame owner)
    {
        game=owner;art=GetComponent<SpriteRenderer>();route=new Vector3[RoutePixels.Length];
        for(int i=0;i<route.Length;i++)route[i]=new Vector3(RoutePixels[i].x/48-16,12-RoutePixels[i].y*24/1024,0);
        transform.position=route[0];next=1;waitUntil=Time.time+2;
    }
    private void Update()
    {
        if(game==null||art==null||route==null)return;
        if(game.BlockMovement||Time.time<waitUntil||Vector2.Distance(transform.position,game.Player.position)<1.2f)
        {distance=0;Show(-1);return;}
        Vector3 before=transform.position;
        transform.position=game.MoveResident(transform,Vector3.MoveTowards(before,route[next],.75f*Time.deltaTime));
        Vector2 delta=transform.position-before;
        if(delta.sqrMagnitude>.000001f)
        {
            direction=PlayerMovement.FacingIndex(delta);distance+=delta.magnitude;stalled=0;
            Show(Mathf.FloorToInt(distance/.16f)%4);
        }
        else{stalled+=Time.deltaTime;Show(-1);}
        if(Vector2.Distance(transform.position,route[next])<.05f)
        {
            next=(next+1)%route.Length;waitUntil=Time.time+Random.Range(2f,4.5f);distance=0;
        }
        else if(stalled>2){waitUntil=Time.time+1;stalled=0;}
    }
    private void Show(int step){art.sprite=RpgLibrarianArtwork.Frame(direction,step);art.flipX=direction==2;}
}

public sealed partial class RpgGame
{
    private void BuildLibrarian()
    {
        if(State.place!=RpgPlace.Library)return;
        var obj=new GameObject("Knihovnice");obj.transform.localScale=Vector3.one*.9f;
        var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=RpgLibrarianArtwork.Frame(0,-1);renderer.sortingOrder=2;
        var label=AddLabel(obj.transform,"Knihovnice",new Vector3(0,.9f,0),new Color(1,.9f,.58f));
        obj.AddComponent<RpgNpcNameplate>().Initialize(this,label,"Knihovnice");
        obj.AddComponent<RpgLibrarianRoutine>().Initialize(this);
        worldItems.Add(new WorldItem{id="librarian",kind="villager",title="Knihovnice",obj=obj});worldObjects.Add(obj);
    }
}

public sealed partial class RpgInterface
{
    public void ShowLibrarian()
    {
        Open("resident","KNIHOVNICE","Vítej v knihovně. Klidně si prohlédni regály, jen prosím vracej knihy na své místo. Některé tady ukrývají víc než pouhá slova…");
        Action("Děkuji, porozhlédnu se",Close);
    }
}
