using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgGame
{
    private Sprite townMap;
    private Vector3 townMapPosition, townMapScale;
    private Material townMapMaterial;
    private static Material locationMapMaterial;
    private RpgPlace? trackedLocation;
    private bool trackedShop;
    public bool InTown => State==null || State.place==RpgPlace.Town;
    public string LocationName => RpgLocations.Name(State.place);

    private void CaptureTownMap()
    {
        if(map==null)return;
        townMap=map.sprite;townMapPosition=map.transform.position;townMapScale=map.transform.localScale;townMapMaterial=map.sharedMaterial;
    }
    private void ApplyLocationMap()
    {
        trackedLocation=null;trackedShop=false;
        State.place=RpgLocations.Normalize(State.place);
        if(map==null)return;
        if(InTown||IsTestLocation)
        {
            map.sprite=townMap;map.transform.position=townMapPosition;map.transform.localScale=townMapScale;map.sharedMaterial=townMapMaterial;
        }
        else
        {
            map.sprite=RpgLocationArt.Map(State.place);map.transform.position=new Vector3(0,0,1);map.transform.localScale=Vector3.one;
            if((State.place==RpgPlace.Library||State.place==RpgPlace.SecretLibrary)&&map.sprite!=null)
                map.transform.localScale=new Vector3(32/map.sprite.bounds.size.x,24/map.sprite.bounds.size.y,1);
            if(State.place==RpgPlace.SecretLibrary)map.transform.localScale*=RpgLocations.SecretRoomScale;
            if(locationMapMaterial==null)locationMapMaterial=new Material(Shader.Find("Sprites/Default"));
            map.sharedMaterial=State.place==RpgPlace.SecretLibrary?RpgVisualTheme.Art(false,4):locationMapMaterial;
            if(map.sprite!=null)map.sprite.texture.filterMode=FilterMode.Point;
        }
    }
    private Vector3 ExitPosition(RpgLocationExit exit) => exit.from==RpgPlace.Town
        ? RpgWorldGeometry.ExpandedPixelPosition(exit.point,map.bounds) : new Vector3(exit.point.x,exit.point.y,transform.position.z);
    private void AddLocationExits()
    {
        foreach(var exit in RpgLocations.Exits)
        {
            if(exit.from!=State.place)continue;
            if(exit.to==RpgPlace.SecretLibrary&&!State.librarySecretOpened)continue;
            AddEntranceMarker(((int)exit.to).ToString(),RpgLocations.Name(exit.to),"locationExit",ExitPosition(exit));
        }
    }
    private bool BuildExplorationLocation()
    {
        if(InTown)return false;
        companionObject=null;AddLocationExits();BuildRitualSite();BuildCycleExploration();BuildLibrarySecret();return true;
    }
    public void TrackLocation(RpgPlace destination)
    {
        var link=RpgLocations.Find(State.place,destination);
        if(destination==RpgPlace.SecretLibrary&&!State.librarySecretOpened){Notify("Tudy zatím žádná cesta nevede.");return;}
        if(link==null){Notify("Do této lokace odsud nevede přímá cesta.");return;}
        trackedLocation=destination;trackedShop=false;Interface.Close();Notify(link.directions+" • u zlaté značky stiskni E.",7);
    }
    public void TrackShop()
    {
        if(!InTown)return;
        trackedShop=true;trackedLocation=null;Interface.Close();Notify("Obchod je označený na mapce. Přijdi ke dveřím a stiskni E.",7);
    }
    private bool TryLocationGoal(out Vector3 position,out string title)
    {
        position=transform.position;title="";
        if(IsTestLocation)return false;
        if(trackedShop&&InTown){position=ShopApproach;title="Obchod • Eliška";return true;}
        RpgLocationExit link=trackedLocation.HasValue?RpgLocations.Find(State.place,trackedLocation.Value):null;
        if(link==null && State.ritualKnown)
        {
            if(State.place==RpgPlace.ChurchRuins){position=RitualPosition;title=State.CellarObjective;return true;}
            if(State.candles<State.RequiredCandles && State.cycle>=2)
            {
                if(State.place==RpgPlace.Forest && !State.forestCandleTaken){position=new Vector3(1,5);title="Svíčka v lese";return true;}
                if(State.place==RpgPlace.Library && !State.libraryCandleTaken)
                {
                    position=State.librarySecretOpened?new Vector3(11.4f,3.7f):new Vector3(11.4f,5.1f);
                    title=State.librarySecretOpened?"Tajný průchod v knihovně":"Kniha se skrytým mechanismem";return true;
                }
                if(State.place==RpgPlace.SecretLibrary && !State.libraryCandleTaken)
                {position=new Vector3(-9.5f,1.65f)*RpgLocations.SecretRoomScale;title="Svíčka v tajném archivu";return true;}
                if(InTown)link=RpgLocations.Find(State.place,!State.forestCandleTaken?RpgPlace.Forest:RpgPlace.Library);
            }
            else if(InTown)link=RpgLocations.Find(State.place,RpgPlace.ChurchRuins);
        }
        if(link==null&&!InTown)
            foreach(var candidate in RpgLocations.Exits)if(candidate.from==State.place&&(candidate.to!=RpgPlace.SecretLibrary||State.librarySecretOpened)){link=candidate;break;}
        if(link==null)return false;
        position=SnapToRoad(ExitPosition(link));title="Cesta: "+RpgLocations.Name(link.to);return true;
    }
    public bool TryTravelLocation(RpgPlace destination)
    {
        var exit=RpgLocations.Find(State.place,destination);
        if(destination==RpgPlace.SecretLibrary&&!State.librarySecretOpened)return false;
        if(IsTestLocation||exit==null||State.health<=0||IsEnteringRoom||Interface.InRoom)return false;
        if(!RpgEntranceRules.InReach(transform.position,SnapToRoad(ExitPosition(exit))))
        {Notify("Přijdi blíž ke zlaté značce přechodu.");return false;}
        StartCoroutine(TravelLocation(exit));return true;
    }
    private IEnumerator TravelLocation(RpgLocationExit exit)
    {
        Interface.Close();IsEnteringRoom=true;
        yield return Interface.FadeLocation(0,1);
        // The shelf closes behind the player; the book can reveal it again next visit.
        if(exit.from==RpgPlace.SecretLibrary&&exit.to==RpgPlace.Library)
            State.librarySecretOpened=false;
        State.place=exit.to;State.RecordArchivePlace(RpgArchive.PlaceKey(exit.to));ApplyLocationMap();
        Vector3 arrival=exit.to==RpgPlace.Town?RpgWorldGeometry.ExpandedPixelPosition(exit.arrival,map.bounds):new Vector3(exit.arrival.x,exit.arrival.y,transform.position.z);
        arrival=SnapToRoad(arrival);State.x=arrival.x;State.y=arrival.y;
        trackedLocation=null;trackedShop=false;
        PlacePlayer();BuildWorld();
        yield return Interface.FadeLocation(1,0);
        IsEnteringRoom=false;Save(false);Notify(LocationName+" • "+RpgLocations.Description(State.place),7);
    }
    public Vector3 MoveInCurrentLocation(Vector3 current,Vector3 desired) => InTown||IsTestLocation
        ? (map!=null?RpgWorldGeometry.Move(current,desired,map.bounds):desired) : RpgLocations.Move(State.place,current,desired);
}

public sealed partial class RpgInterface
{
    private Image locationFade;
    public IEnumerator FadeLocation(float from,float to)
    {
        if(locationFade==null)
        {
            var rect=Rect(canvas,"Přechod lokace",Vector2.zero,Vector2.zero);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            locationFade=rect.gameObject.AddComponent<Image>();
        }
        locationFade.gameObject.SetActive(true);locationFade.transform.SetAsLastSibling();
        float elapsed=0;
        while(elapsed<RpgEntranceRules.FadeSeconds)
        {elapsed+=Time.unscaledDeltaTime;locationFade.color=new Color(0,0,0,Mathf.Lerp(from,to,elapsed/RpgEntranceRules.FadeSeconds));yield return null;}
        locationFade.color=new Color(0,0,0,to);if(to==0)locationFade.gameObject.SetActive(false);
    }
    private void ShowLocationMap()
    {
        Open("map","LOKACE • "+game.LocationName.ToUpperInvariant(),
            "HAVRANOV\n├ Obchod → sklep obchodu\n├ Knihovna\n├ Zřícenina kostela\n└ Les → Temný les\n\n"+
            RpgLocations.Description(game.State.place)+"\n\nVyber cestu pro označení vstupu na mapce. U vstupu stiskni E.");
        modalBody.fontSize=26;
        foreach(var link in RpgLocations.Exits)
        {
            if(link.from!=game.State.place)continue;
            if(link.to==RpgPlace.SecretLibrary&&!game.State.librarySecretOpened)continue;
            var destination=link.to;Action("Označit cestu: "+RpgLocations.Name(destination),()=>game.TrackLocation(destination));
        }
        if(game.InTown)Action("Označit cestu: obchod",game.TrackShop);
        Action("Zavřít [M / Esc]",Close);
    }
}
