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
    public const float DoorHalfWidth=.55f;
    public static bool CanUse(Vector2 player,Vector2 door,Vector2? approach=null)
    {
        if(!InReach(player,door))return false;
        if(!approach.HasValue)return true;
        var facing=approach.Value-door;if(facing.sqrMagnitude<.0001f)return true;
        facing.Normalize();var offset=player-door;
        return Vector2.Dot(offset,facing)>=-.05f && Mathf.Abs(offset.x*facing.y-offset.y*facing.x)<=DoorHalfWidth;
    }
    public static bool IsEntrance(string kind) => kind=="shop" || kind=="locationExit" || kind=="secretRoom";
    public static string Prompt(string title) => "[E] Vstoupit: "+title;
}

public static class RpgInteractionRules
{
    public const float NpcReach=1f;
    public const float ItemReach=.75f;
    public const float RitualReach=1.3f;

    public static float Reach(string kind)
    {
        if(RpgEntranceRules.IsEntrance(kind))return RpgEntranceRules.Reach;
        if(kind=="ritual")return RitualReach;
        if(kind=="secretBook")return RpgSecretBookGlow.BookReach;
        if(kind=="mayor" || kind=="healer" || kind=="guard" || kind=="elder" ||
            kind=="villager" || kind=="district" || kind=="librarian" || kind=="merchant")
            return NpcReach;
        return ItemReach;
    }
}

public sealed partial class RpgGame
{
    private void AddEntranceMarker(string id,string title,string kind,Vector3 position,Vector3? approach=null)
    {
        var obj=new GameObject("Vstup • "+title);obj.transform.position=position;
        var art=obj.AddComponent<SpriteRenderer>();art.sprite=RpgVisualTheme.Icon("entrance");art.color=Color.white;art.sortingOrder=5;
        obj.transform.localScale=Vector3.one*RpgEntranceRules.MarkerScale;
        var trigger=obj.AddComponent<CircleCollider2D>();trigger.radius=RpgEntranceRules.Reach/RpgEntranceRules.MarkerScale;trigger.isTrigger=true;
        var label=AddLabel(obj.transform,RpgEntranceRules.Prompt(title),new Vector3(0,1,0),new Color(1,.9f,.65f));
        label.transform.localScale=Vector3.one/RpgEntranceRules.MarkerScale;label.characterSize=.05f;
        worldItems.Add(new WorldItem{id=id,kind=kind,title=title,obj=obj,entranceApproach=approach});worldObjects.Add(obj);
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
        if(exit==null || !CanUseExit(exit))return;
        if(!State.CanAccessLocation(destination)){Interface.ShowLockedForest();return;}
        if(destination==RpgPlace.Forest && !State.forestDiscoverySeen){Interface.ShowForestDiscovery();return;}
        Interface.ShowEntrance(RpgLocations.Name(destination),()=>TryTravelLocation(destination));
    }

}

public sealed partial class RpgInterface
{
    public void ShowLockedForest()
    {
        Open("cellarClue","CESTA DO LESA","Hmmm, já zatím nevím, jak se tam dostat.");
        Action("Zůstat zde",Close);
    }
    public void ShowForestDiscovery()
    {
        if(!game.State.ForestUnlocked){ShowLockedForest();return;}
        var s=game.State;
        string thought=s.heroIsBoy?"Já už jsem asi přišel na to, jak se tam dostat…":"Já už jsem asi přišla na to, jak se tam dostat…";
        string defence=s.swordTrained?"Boj s mečem už znám. Výbavu si ale musím znovu pořídit.":
            s.heroIsBoy?"A teď bych se potřeboval naučit bránit. Zkusím požádat Petra o výcvik s mečem.":"A teď bych se potřebovala naučit bránit. Zkusím požádat Petra o výcvik s mečem.";
        Open("cellarClue","NOVÁ CESTA",thought+"\n\nOTEVŘENA NOVÁ LOKACE • LES\n\n"+defence+
            (s.cycle>=3?"\n\nTentokrát jsou v lese také netopýři. Proti nim budu potřebovat luk.":""));
        Action("Pokračovat",()=>
        {
            s.forestDiscoverySeen=true;game.Save(false);
            ShowEntrance("Les",()=>game.TryTravelLocation(RpgPlace.Forest));
        });
    }
    private Action entranceCancel;
    public void ShowEntrance(string title,Action enter,Action stay=null)
    {
        Open("entrance","VSTUP • "+title.ToUpperInvariant(),"Chceš vstoupit?\n\n"+title);
        entranceCancel=stay??CloseImmediately;
        Action("Vstoupit",()=>{entranceCancel=null;enter();});
        Action("Zůstat zde",Close);
    }
}
