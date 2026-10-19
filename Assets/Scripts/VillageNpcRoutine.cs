using UnityEngine;

// Residents walk their daily route on the same roads as the player, then pause to work.
public sealed class VillageNpcRoutine : MonoBehaviour
{
    private RpgGame game;
    private Vector3[] route;
    private string title,activity;
    private int next;
    private float waitUntil, stalledFor;
    private TextMesh label;
    private bool isMatej;
    private bool isPetr;
    private bool isBlacksmith;
    private SpriteRenderer matejRenderer;
    private Vector2 matejFacing=Vector2.down;
    private float matejDistance;
    public void Initialize(RpgGame owner,string role,string name)
    {
        game=owner;title=name;label=GetComponentInChildren<TextMesh>();
        isMatej=role=="elder";isPetr=role=="guard";isBlacksmith=name.Contains("Kovář");if(isMatej||isPetr||isBlacksmith)matejRenderer=GetComponent<SpriteRenderer>();
        if(role=="district")
        {
            Vector3 start=transform.position;
            route=new[]{start,game.SnapToRoad(start+Vector3.right*1.2f),game.SnapToRoad(start+Vector3.up*1.2f)};
            activity=name.Contains("Kovář")?"kuje podkovy":name.Contains("Mlynář")?"kontroluje mouku":name.Contains("Rybář")?"opravuje sítě":name.Contains("Selka")?"přebírá zeleninu":name.Contains("Trhovkyně")?"rovná zboží":"hlídá bránu";
            waitUntil=Time.time+Random.Range(2f,5f);return;
        }
        Vector2[] points;
        if(role=="elder")
        {
            Vector3 start=transform.position;
            route=new[]{start,game.SnapToRoad(start+Vector3.right*.8f),game.SnapToRoad(start+Vector3.down*.7f)};
            activity="odpočívá na náměstí";waitUntil=Time.time+Random.Range(3f,7f);return;
        }
        if(role=="guard") { points=new[]{new Vector2(1130,530),new Vector2(1235,530),new Vector2(1300,505),new Vector2(1390,477)};activity="hlídá cestu"; }
        else if(role=="healer") { points=new[]{new Vector2(700,630),new Vector2(670,580),new Vector2(720,540),new Vector2(760,620)};activity="třídí léčivé byliny"; }
        else if(role=="villager")
        {
            points=new[]{new Vector2(710,490),new Vector2(800,510),new Vector2(830,600),new Vector2(740,620)};
            if(name.Contains("Pekař"))activity="připravuje chléb";
            else if(name.Contains("Tesař"))activity="opravuje stánek";
            else if(name.Contains("Selka"))activity="přebírá zeleninu";
            else if(name.Contains("Kupec"))activity="rovná zboží";
            else if(name.Contains("Mlynář"))activity="kontroluje pytle mouky";
            else activity="zašívá látku";
            next=Random.Range(0,points.Length);
        }
        else { points=new[]{new Vector2(710,490),new Vector2(800,510),new Vector2(830,600),new Vector2(740,620)};activity="kontroluje tržiště"; }
        var map=GameObject.Find("Background").GetComponent<SpriteRenderer>().bounds;
        route=new Vector3[points.Length];
        for(int i=0;i<points.Length;i++)route[i]=game.SnapToRoad(RpgWorldGeometry.PixelPosition(points[i],map));
        waitUntil=Time.time+Random.Range(2f,5f);
    }
    private void Update()
    {
        if(game==null || game.BlockMovement || route==null){MatejPose(Vector2.zero);return;}
        if(Vector2.Distance(transform.position,game.Player.position)<1.2f)
        { if(label!=null)label.text=title+"\n[E] Promluvit";MatejPose(Vector2.zero);return; }
        if(Time.time<waitUntil) { if(label!=null)label.text=title+"\n"+activity;MatejPose(Vector2.zero);return; }
        if(label!=null)label.text=title;
        Vector3 before=transform.position;
        transform.position=game.MoveResident(transform,Vector3.MoveTowards(before,route[next],Time.deltaTime*.7f));
        MatejPose(transform.position-before);
        stalledFor=Vector2.Distance(before,transform.position)<.001f?stalledFor+Time.deltaTime:0;
        if(Vector2.Distance(transform.position,route[next])<.15f || stalledFor>2)
        { next=(next+1)%route.Length;waitUntil=Time.time+Random.Range(3f,7f);stalledFor=0; }
    }
    private void MatejPose(Vector2 movement)
    {
        if((!isMatej&&!isPetr&&!isBlacksmith)||matejRenderer==null)return;
        bool moving=movement.sqrMagnitude>.000001f;
        if(moving){matejFacing=movement;matejDistance+=movement.magnitude;}else matejDistance=0;
        int direction=PlayerMovement.FacingIndex(matejFacing);
        int step=moving?PlayerMovement.DirectionalWalkingFrame(matejDistance):0;
        matejRenderer.sprite=isBlacksmith?RpgDirectionalHero.BlacksmithFrame(direction,step):isPetr?RpgDirectionalHero.PetrFrame(direction,step):RpgDirectionalHero.MatejFrame(direction,step);
        matejRenderer.flipX=direction==2;
    }
}
