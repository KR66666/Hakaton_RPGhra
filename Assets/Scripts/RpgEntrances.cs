using System;
using System.Collections;
using UnityEngine;

// All world doors and nested rooms share the same reach, wording and transition.
public static class RpgEntranceRules
{
    public const float Reach=1.35f;
    public const float FadeSeconds=.2f;
    public const float MarkerScale=.55f;
    public static bool InReach(Vector2 player,Vector2 entrance) => Vector2.Distance(player,entrance)<=Reach;
    public static bool IsEntrance(string kind) => kind=="shop" || kind=="locationExit" || kind=="secretRoom";
    public static string Prompt(string title) => "[E] Vstoupit: "+title;
}

public sealed partial class RpgGame
{
    private void AddEntranceMarker(string id,string title,string kind,Vector3 position)
    {
        var obj=new GameObject("Vstup • "+title);obj.transform.position=SnapToRoad(position);
        var art=obj.AddComponent<SpriteRenderer>();art.sprite=RpgVisualTheme.Icon("entrance");art.color=Color.white;art.sortingOrder=5;
        obj.transform.localScale=Vector3.one*RpgEntranceRules.MarkerScale;
        var trigger=obj.AddComponent<CircleCollider2D>();trigger.radius=RpgEntranceRules.Reach/RpgEntranceRules.MarkerScale;trigger.isTrigger=true;
        var label=AddLabel(obj.transform,RpgEntranceRules.Prompt(title),new Vector3(0,1,0),new Color(1,.9f,.65f));
        label.transform.localScale=Vector3.one/RpgEntranceRules.MarkerScale;label.characterSize=.05f;
        worldItems.Add(new WorldItem{id=id,kind=kind,title=title,obj=obj});worldObjects.Add(obj);
    }
    public void BeginMiniTransition(Action arrive)
    {
        if(IsEnteringRoom || InBattle || State.health<=0)return;
        StartCoroutine(MiniTransition(arrive));
    }
    private IEnumerator MiniTransition(Action arrive)
    {
        IsEnteringRoom=true;
        yield return Interface.FadeLocation(0,1);
        arrive();
        yield return Interface.FadeLocation(1,0);
        IsEnteringRoom=false;
    }
    private void OfferLocationEntry(RpgPlace destination)
    {
        var exit=RpgLocations.Find(State.place,destination);
        if(exit==null || !RpgEntranceRules.InReach(transform.position,SnapToRoad(ExitPosition(exit))))return;
        Interface.ShowEntrance(RpgLocations.Name(destination),()=>TryTravelLocation(destination));
    }
    private void OfferSecretEntry()
    {
        var entrance=worldItems.Find(item=>item.kind=="secretRoom" && item.obj!=null);
        if(entrance==null || !RpgEntranceRules.InReach(transform.position,entrance.obj.transform.position))return;
        Interface.ShowEntrance("Tajný pokoj knihovny",()=>BeginMiniTransition(Interface.ShowSecretRoom));
    }
}

public sealed partial class RpgInterface
{
    private Action entranceCancel;
    public void ShowEntrance(string title,Action enter,Action stay=null)
    {
        Open("entrance","VSTUP • "+title.ToUpperInvariant(),"Chceš vstoupit?\n\n"+title);
        entranceCancel=stay??CloseImmediately;
        Action("Vstoupit",()=>{entranceCancel=null;enter();});
        Action("Zůstat zde",Close);
    }
}
