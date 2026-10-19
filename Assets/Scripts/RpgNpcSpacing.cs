using UnityEngine;

public static class RpgNpcSpacing
{
    // Separation follows actor silhouettes, not the full transparent sprite bounds.
    public const float ResidentSeparation=.55f;
    public const float PlayerSeparation=.55f;
    public const float PlayerStopDistance=.7f;
    public static bool CanStep(Vector2 current,Vector2 desired,Vector2 other,float separation=ResidentSeparation)
    {
        float previous=(current-other).sqrMagnitude;
        if(previous<separation*separation)
            return (desired-other).sqrMagnitude>previous; // Let an existing overlap separate.
        Vector2 step=desired-current;
        float t=step.sqrMagnitude>.000001f?Mathf.Clamp01(Vector2.Dot(other-current,step)/step.sqrMagnitude):0;
        return (current+step*t-other).sqrMagnitude>=separation*separation;
    }
}

public sealed class RpgNpcStuckWatch
{
    private Vector2 anchor;
    private float elapsed;
    public void Reset(Vector2 position){anchor=position;elapsed=0;}
    public bool Tick(Vector2 position,float deltaTime)
    {
        // Measure accumulated movement, not a per-frame step: high frame rates
        // must not make a slowly moving NPC appear stationary.
        if(Vector2.Distance(anchor,position)>=.06f){Reset(position);return false;}
        elapsed+=Mathf.Max(0,deltaTime);
        if(elapsed<=2)return false;
        Reset(position);return true;
    }
}

public sealed partial class RpgGame
{
    public Vector3 MoveResident(Transform resident,Vector3 desired)
    {
        Vector3 current=resident.position;
        desired=MoveEnemy(current,desired);
        return CanPassResidents(current,desired,resident,RpgNpcSpacing.ResidentSeparation) &&
            RpgNpcSpacing.CanStep(current,desired,transform.position,RpgNpcSpacing.PlayerSeparation)?desired:current;
    }
    private bool CanPassResidents(Vector3 current,Vector3 desired,Transform ignore,float separation)
    {
        foreach(var item in worldItems)
        {
            if(item.obj==null||!item.obj.activeInHierarchy||item.obj.transform==ignore)continue;
            if(item.kind!="mayor"&&item.kind!="merchant"&&item.kind!="healer"&&item.kind!="guard"&&item.kind!="elder"&&item.kind!="villager"&&item.kind!="district"&&item.kind!="librarian")continue;
            if(!RpgNpcSpacing.CanStep(current,desired,item.obj.transform.position,separation))return false;
        }
        return true;
    }
    public Vector3 MovePlayer(Vector3 current,Vector3 desired)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(current,desired)/.06f));
        Vector3 delta=(desired-current)/steps;
        for(int i=0;i<steps;i++)
        {
            var next=MoveInCurrentLocation(current,current+new Vector3(delta.x,0,0));
            if(CanPassResidents(current,next,null,RpgNpcSpacing.PlayerSeparation))current.x=next.x;
            next=MoveInCurrentLocation(current,current+new Vector3(0,delta.y,0));
            if(CanPassResidents(current,next,null,RpgNpcSpacing.PlayerSeparation))current.y=next.y;
        }
        return current;
    }
}
