using UnityEngine;

// Residents walk their daily route on the same roads as the player, then pause to work.
public sealed class VillageNpcRoutine : MonoBehaviour
{
    private RpgGame game;
    private Vector3[] route;
    private string title,activity;
    private int next;
    private float waitUntil;
    private int routeDirection=1;
    private bool detouring;
    private Vector3 detourTarget;
    private readonly RpgNpcStuckWatch stuckWatch=new RpgNpcStuckWatch();
    private TextMesh label;
    public void Initialize(RpgGame owner,string role,string name)
    {
        game=owner;title=name;label=GetComponentInChildren<TextMesh>();
        stuckWatch.Reset(transform.position);
        var points=RpgNpcPatrols.Points(role,name);
        var map=GameObject.Find("Background").GetComponent<SpriteRenderer>().bounds;
        route=new Vector3[points.Length];
        float closest=float.MaxValue;
        for(int i=0;i<points.Length;i++)
        {
            route[i]=game.SnapToRoad(RpgWorldGeometry.ExpandedPixelPosition(points[i],map));
            float distance=Vector2.Distance(transform.position,route[i]);
            if(distance<closest){closest=distance;next=i;}
        }
        routeDirection=Random.value<.5f?-1:1;
        activity=role=="guard"?"hlídá cestu":role=="healer"?"připravuje obvazy":
            name.Contains("Kovář")?"kuje podkovy":name.Contains("Mlynář")?"kontroluje mouku":
            name.Contains("Rybář")?"opravuje sítě":name.Contains("Selka")?"přebírá zeleninu":
            name.Contains("Trhovkyně")?"rovná zboží":"odpočívá u cesty";
        waitUntil=Time.time+Random.Range(.5f,1.5f);
    }
    private void Update()
    {
        if(game==null || route==null)return;
        if(game.BlockMovement){stuckWatch.Reset(transform.position);return;}
        if(Time.time<waitUntil) { stuckWatch.Reset(transform.position);if(label!=null)label.text=title+"\n"+activity;return; }
        if(label!=null)label.text=title+(Vector2.Distance(transform.position,game.Player.position)<1.2f?"\n[E] Promluvit":"");
        Vector3 before=transform.position;
        Vector3 target=detouring?detourTarget:route[next];
        transform.position=game.MoveResident(transform,Vector3.MoveTowards(before,target,Time.deltaTime*.9f));
        if(Vector2.Distance(transform.position,target)<.15f)
        {
            stuckWatch.Reset(transform.position);
            if(detouring){detouring=false;return;}
            next=(next+routeDirection+route.Length)%route.Length;waitUntil=Time.time+Random.Range(1.5f,3.5f);return;
        }
        if(stuckWatch.Tick(transform.position,Time.deltaTime))ChangeWalkingDirection(target-before);
    }
    private void ChangeWalkingDirection(Vector2 blockedDirection)
    {
        routeDirection=-routeDirection;
        next=(next+routeDirection+route.Length)%route.Length;
        detouring=false;waitUntil=0;
        Vector2 back=blockedDirection.sqrMagnitude>.0001f?-blockedDirection.normalized:Vector2.left;
        // Prefer walking back; if that is blocked, try the sides. Every probe
        // uses the same terrain and actor collision rules as normal walking.
        foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f,180f})
        {
            Vector3 direction=Quaternion.Euler(0,0,angle)*(Vector3)back;
            Vector3 candidate=game.MoveResident(transform,transform.position+direction*.9f);
            if(Vector2.Distance(candidate,transform.position)<.2f)continue;
            detourTarget=candidate;detouring=true;break;
        }
        stuckWatch.Reset(transform.position);
    }
}
