using System.Collections;
using UnityEngine;

public sealed partial class RpgGame
{
    private static Material ritualMaterial;
    private static readonly Vector3 RitualPosition=Vector3.zero;
    private readonly Vector3[] ritualCandles={new Vector3(0,2.3f),new Vector3(-2,-1.4f),new Vector3(2,-1.4f)};
    private void BuildRitualSite()
    {
        if(State.place!=RpgPlace.ChurchRuins)return;
        var site=new GameObject("Rituální trojúhelník");worldObjects.Add(site);
        var line=site.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.loop=true;line.positionCount=3;line.widthMultiplier=.09f;
        if(ritualMaterial==null)ritualMaterial=new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial=ritualMaterial;
        line.startColor=line.endColor=new Color(.7f,.46f,.22f);line.sortingOrder=3;
        line.SetPositions(new[]{ritualCandles[0],ritualCandles[1],ritualCandles[2]});
        for(int i=0;i<RpgState.FirstCycleCandles;i++)
        {
            var candle=new GameObject("Místo pro svíčku "+i);candle.transform.SetParent(site.transform);candle.transform.position=ritualCandles[i];
            var art=candle.AddComponent<SpriteRenderer>();art.sprite=MarkerSprite();art.color=new Color(.65f,.57f,.4f);art.sortingOrder=4;candle.transform.localScale=Vector3.one*.22f;
        }
        var trigger=site.AddComponent<CircleCollider2D>();trigger.isTrigger=true;trigger.radius=1.3f;
        AddLabel(site.transform,"[E] Rituální obrazec",new Vector3(0,-2,0),new Color(.9f,.8f,.55f));
        worldItems.Add(new WorldItem{id="ritual",kind="ritual",title="Prozkoumat rituální obrazec",obj=site});
    }
    public void TryRitual()
    {
        if(IsEnteringRoom || State.health<=0 || State.place!=RpgPlace.ChurchRuins || Vector2.Distance(transform.position,RitualPosition)>2.5f)return;
        Interface.ShowRitual();
    }
    public void StartRitual()
    {
        if(IsEnteringRoom || !State.CanPerformRitual || Vector2.Distance(transform.position,RitualPosition)>2.5f)return;
        StartCoroutine(PerformRitual());
    }
    private IEnumerator PerformRitual()
    {
        var next=State.BeginSecondCycle();if(next==null)yield break;
        Interface.Close();IsEnteringRoom=true;
        Notify("Pokládáš tři svíčky na obrazec…",4);
        var site=worldItems.Find(i=>i.kind=="ritual")?.obj;
        if(site!=null)
            foreach(var candle in site.GetComponentsInChildren<SpriteRenderer>())
            {
                candle.color=new Color(1,.7f,.16f);candle.transform.localScale=Vector3.one*.35f;
                yield return new WaitForSecondsRealtime(.55f);
            }
        Notify("Světlo sílí. Něco je špatně…",4);
        if(site!=null){var line=site.GetComponent<LineRenderer>();line.startColor=line.endColor=new Color(.85f,.07f,.04f);}
        yield return new WaitForSecondsRealtime(2);
        State.health=0;playerRenderer.color=new Color(.4f,.05f,.05f);
        Notify("Rituál selhal. Umíráš.",3);
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Interface.FadeLocation(0,1);
        RestoreState(next);Save(false,true);
        yield return new WaitForSecondsRealtime(.7f);
        yield return Interface.FadeLocation(1,0);
        IsEnteringRoom=false;Interface.ShowSecondCycle();
    }
}

public sealed partial class RpgInterface
{
    public void ShowRitual()
    {
        var s=game.State;
        Open("cellarClue","RITUÁLNÍ OBRAZEC",s.CanPerformRitual?
            "Trojúhelník a tři místa pro svíčky. Podle průvodce zde musíš svíčky rozmístit a zapálit, aby ses probudil ze snu.":
            s.cycle==2?"Tento obrazec znáš. První rituál tě zabil a smyčka začala znovu.":
            s.candles!=RpgState.FirstCycleCandles?"Na obrazci jsou tři místa pro svíčky. Nejdřív je musíš najít.":"Svíčky máš. Nejdřív se vrať k průvodci pro radu k rituálu.");
        if(s.CanPerformRitual)Action("Rozmístit svíčky a začít rituál",game.StartRitual);
        Action("Odejít",Close);
    }
    public void ShowSecondCycle()
    {
        Open("cellarClue","DRUHÝ CYKLUS • PROBUZENÍ","Prudce se nadechneš. Znovu jsi na začátku. Rituál skončil smrtí, ale ze snu ses nedostal. Svíčky zmizely. Vzpomínku na první cyklus si uchováváš.");
        Action("Vstát a pokračovat",Close);
    }
}
