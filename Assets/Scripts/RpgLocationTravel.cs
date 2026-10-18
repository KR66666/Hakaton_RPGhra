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
            if(locationMapMaterial==null)locationMapMaterial=new Material(Shader.Find("Sprites/Default"));
            map.sharedMaterial=locationMapMaterial;
        }
    }
    private Vector3 ExitPosition(RpgLocationExit exit) => exit.from==RpgPlace.Town
        ? RpgWorldGeometry.ExpandedPixelPosition(exit.point,map.bounds) : new Vector3(exit.point.x,exit.point.y,transform.position.z);
    private void AddLocationExits()
    {
        foreach(var exit in RpgLocations.Exits)
        {
            if(exit.from!=State.place)continue;
            var obj=new GameObject("Přechod • "+RpgLocations.Name(exit.to));obj.transform.position=SnapToRoad(ExitPosition(exit));
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=MarkerSprite();renderer.color=new Color(1,.79f,.27f);renderer.sortingOrder=5;
            obj.transform.localScale=Vector3.one*.55f;
            var trigger=obj.AddComponent<CircleCollider2D>();trigger.radius=.8f;trigger.isTrigger=true;
            var label=AddLabel(obj.transform,"[E] "+RpgLocations.Name(exit.to),new Vector3(0,1,0),new Color(1,.9f,.65f));
            label.transform.localScale=Vector3.one/.55f;label.characterSize=.04f;
            worldItems.Add(new WorldItem{id=((int)exit.to).ToString(),kind="locationExit",title="Přejít: "+RpgLocations.Name(exit.to),obj=obj});
            worldObjects.Add(obj);
        }
    }
    private bool BuildExplorationLocation()
    {
        if(InTown)return false;
        companionObject=null;AddLocationExits();return true;
    }
    public void TrackLocation(RpgPlace destination)
    {
        var link=RpgLocations.Find(State.place,destination);
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
        if(link==null&&!InTown)
            foreach(var candidate in RpgLocations.Exits)if(candidate.from==State.place){link=candidate;break;}
        if(link==null)return false;
        position=SnapToRoad(ExitPosition(link));title="Cesta: "+RpgLocations.Name(link.to);return true;
    }
    public bool TryTravelLocation(RpgPlace destination)
    {
        var exit=RpgLocations.Find(State.place,destination);
        if(IsTestLocation||exit==null||State.health<=0||IsEnteringRoom||Interface.InRoom)return false;
        if(Vector2.Distance(transform.position,SnapToRoad(ExitPosition(exit)))>1.35f)
        {Notify("Přijdi blíž ke zlaté značce přechodu.");return false;}
        StartCoroutine(TravelLocation(exit));return true;
    }
    private IEnumerator TravelLocation(RpgLocationExit exit)
    {
        Interface.Close();IsEnteringRoom=true;
        yield return Interface.FadeLocation(0,1);
        State.place=exit.to;ApplyLocationMap();
        Vector3 arrival=exit.to==RpgPlace.Town?RpgWorldGeometry.ExpandedPixelPosition(exit.arrival,map.bounds):new Vector3(exit.arrival.x,exit.arrival.y,transform.position.z);
        arrival=SnapToRoad(arrival);State.x=arrival.x;State.y=arrival.y;
        trackedLocation=null;trackedShop=false;offeredRoom=null;
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
        while(elapsed<.2f)
        {elapsed+=Time.unscaledDeltaTime;locationFade.color=new Color(0,0,0,Mathf.Lerp(from,to,elapsed/.2f));yield return null;}
        locationFade.color=new Color(0,0,0,to);if(to==0)locationFade.gameObject.SetActive(false);
    }
    private void ShowLocationMap()
    {
        Open("map","LOKACE • "+game.LocationName.ToUpperInvariant(),
            "HAVRANOV\n├ Obchod → sklep obchodu\n├ Knihovna\n├ Zřícenina kostela\n└ Les → Temný les\n\n"+
            RpgLocations.Description(game.State.place)+"\n\nVyber cestu pro označení vstupu na mapce. U vstupu stiskni E.");
        modalBody.fontSize=21;
        foreach(var link in RpgLocations.Exits)
        {
            if(link.from!=game.State.place)continue;
            var destination=link.to;Action("Označit cestu: "+RpgLocations.Name(destination),()=>game.TrackLocation(destination));
        }
        if(game.InTown)Action("Označit cestu: obchod",game.TrackShop);
        Action("Zavřít [M / Esc]",Close);
    }
}
