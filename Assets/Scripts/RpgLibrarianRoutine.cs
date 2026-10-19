using UnityEngine;

public sealed class RpgLibrarianRoutine : MonoBehaviour
{
    private RpgGame game;
    private SpriteRenderer art;
    private Vector3[] route;
    private int next,direction,routeDirection=1;
    private float waitUntil,distance;
    private readonly RpgNpcStuckWatch stuckWatch=new RpgNpcStuckWatch();
    public static readonly Vector2[] RoutePixels={
        new Vector2(768,860),new Vector2(768,700),new Vector2(428,700),new Vector2(428,515),
        new Vector2(768,515),new Vector2(1110,515),new Vector2(1110,350),new Vector2(768,350),
        new Vector2(768,515)};
    public static Vector3 RoutePosition(Vector2 pixel) => new Vector3(pixel.x/48-16,12-pixel.y*24/1024,0);
    public void Initialize(RpgGame owner)
    {
        game=owner;art=GetComponent<SpriteRenderer>();route=new Vector3[RoutePixels.Length];
        for(int i=0;i<route.Length;i++)route[i]=RoutePosition(RoutePixels[i]);
        transform.position=route[0];next=1;waitUntil=Time.time+2;
        stuckWatch.Reset(transform.position);
    }
    private void Update()
    {
        if(game==null||art==null||route==null)return;
        if(game.BlockMovement||Time.time<waitUntil||Vector2.Distance(transform.position,game.Player.position)<1.2f)
        {stuckWatch.Reset(transform.position);distance=0;Show(-1);return;}
        Vector3 before=transform.position;
        transform.position=game.MoveResident(transform,Vector3.MoveTowards(before,route[next],.75f*Time.deltaTime));
        Vector2 delta=transform.position-before;
        if(delta.sqrMagnitude>.000001f)
        {
            direction=PlayerMovement.FacingIndex(delta);distance+=delta.magnitude;
            Show(Mathf.FloorToInt(distance/.16f)%4);
        }
        else Show(-1);
        if(Vector2.Distance(transform.position,route[next])<.05f)
        {
            next=(next+routeDirection+route.Length)%route.Length;waitUntil=Time.time+Random.Range(2f,4.5f);distance=0;
            stuckWatch.Reset(transform.position);
        }
        else if(stuckWatch.Tick(transform.position,Time.deltaTime))
        {routeDirection=-routeDirection;next=(next+routeDirection+route.Length)%route.Length;waitUntil=0;}
    }
    private void Show(int step){art.sprite=RpgLibrarianArtwork.Frame(direction,step) ?? art.sprite;art.flipX=direction==2;}
}
