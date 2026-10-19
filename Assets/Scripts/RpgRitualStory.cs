using System.Collections;
using UnityEngine;

public sealed partial class RpgGame
{
    private static Material ritualMaterial;
    private static readonly Vector3 RitualPosition=Vector3.zero;
    private Vector3[] RitualCandles
    {
        get
        {
            int count=State.RequiredCandles;var points=new Vector3[count];
            for(int i=0;i<count;i++){float angle=Mathf.PI/2+i*2*Mathf.PI/count;points[i]=new Vector3(Mathf.Cos(angle)*2.3f,Mathf.Sin(angle)*2.3f);}
            return points;
        }
    }
    private void BuildRitualSite()
    {
        if(State.place!=RpgPlace.ChurchRuins)return;
        var site=new GameObject("Rituální "+State.RitualShape);worldObjects.Add(site);
        var line=site.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.loop=true;line.positionCount=State.RequiredCandles;line.widthMultiplier=.09f;
        if(ritualMaterial==null)ritualMaterial=new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial=ritualMaterial;
        line.startColor=line.endColor=new Color(.7f,.46f,.22f);line.sortingOrder=3;
        var points=RitualCandles;var outline=new Vector3[points.Length];
        for(int i=0;i<points.Length;i++)outline[i]=points[State.cycle==3?i*2%5:i];
        line.SetPositions(outline);
        for(int i=0;i<State.RequiredCandles;i++)
        {
            var candle=new GameObject("Místo pro svíčku "+i);candle.transform.SetParent(site.transform);candle.transform.position=points[i];
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
        if(IsEnteringRoom || !State.CanPerformRitual || !State.CanBeginFinalRitual || Vector2.Distance(transform.position,RitualPosition)>2.5f)return;
        StartCoroutine(PerformRitual());
    }
    private IEnumerator PerformRitual()
    {
        if(State.cycle==3 && State.finalRitualStarted){Interface.ShowFinalWave();yield break;}
        var next=State.cycle<3?State.BeginSecondCycle():null;
        if(State.cycle<3 && next==null)yield break;
        Interface.Close();IsEnteringRoom=true;
        Notify("Pokládáš "+State.RequiredCandles+" svíček na obrazec…",4);
        var site=worldItems.Find(i=>i.kind=="ritual")?.obj;
        if(site!=null)
            foreach(var candle in site.GetComponentsInChildren<SpriteRenderer>())
            {
                candle.color=new Color(1,.7f,.16f);candle.transform.localScale=Vector3.one*.35f;
                yield return new WaitForSecondsRealtime(.55f);
            }
        if(State.cycle==3)
        {
            State.StartFinalWave();IsEnteringRoom=false;BuildWorld();Save(false);Interface.ShowFinalWave();yield break;
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
        if(s.storyComplete){ShowFinalWave();return;}
        if(s.finalRitualStarted){ShowFinalWave();return;}
        Open("cellarClue","RITUÁLNÍ OBRAZEC",$"{s.RitualShape.ToUpperInvariant()} • {s.candles}/{s.RequiredCandles} svíček\n\n"+
            (s.CanPerformRitual?"Máš dost svíček. Můžeš je rozmístit a začít rituál.":s.CellarObjective));
        if(s.CanPerformRitual && s.CanBeginFinalRitual)Action("Rozmístit svíčky a začít rituál",game.StartRitual);
        else if(s.cycle==3 && !s.CanBeginFinalRitual)modalBody.text+="\n\nPřed posledním rituálem se nauč bojovat s mečem i lukem a obě zbraně si opatři. Po začátku už kostel neopustíš.";
        Action("Odejít",Close);
    }
    public void ShowSecondCycle()
    {
        Open("heroShopDialogue",game.State.HeroName.ToUpperInvariant(),game.State.heroIsBoy?"Okay, zkusím to nějak sám.":"Okay, zkusím to nějak sama.");
        Action("Vstát a pokračovat",Close);
    }
}
