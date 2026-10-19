using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RpgState
{
    public bool introSeen;
}

public sealed partial class RpgGame
{
    private IEnumerator PlayDreamIntro()
    {
        IsEnteringRoom=true;
        yield return Interface.PlayDreamIntro();
        State.introSeen=true;IsEnteringRoom=false;Save(false);
        Interface.ShowGuideWelcome();
    }
    private void BuildFinalWaveVisuals()
    {
        if(State.place!=RpgPlace.ChurchRuins || !State.FinalWaveActive)return;
        for(int i=0;i<State.finalEnemiesRemaining;i++)
        {
            float angle=i*Mathf.PI*2/RpgState.FinalWaveSize;
            bool flying=(State.finalEnemiesRemaining-i)%2==0;
            var obj=new GameObject("Obléhání rituálu • "+RpgEnemy.KindName(flying));
            obj.transform.position=SnapToRoad(new Vector3(Mathf.Cos(angle)*6,Mathf.Sin(angle)*6));
            obj.transform.localScale=Vector3.one*.85f;
            var art=obj.AddComponent<SpriteRenderer>();art.sprite=RpgVisualTheme.Icon(flying?"bat":"wolf");art.sortingOrder=4;
            worldObjects.Add(obj);
        }
    }
}

public sealed partial class RpgInterface
{
    // Prototype storyboard: a fixed overhead room, flat bed and actors viewed
    // from above. Only a new story plays it; ritual restarts do not.
    public IEnumerator PlayDreamIntro()
    {
        SetGameplayHudVisible(false);
        var root=Rect(canvas,"Úvod • noční můra",Vector2.zero,Vector2.zero);
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        root.gameObject.AddComponent<Image>().color=new Color(.035f,.025f,.055f);
        var dream=Rect(root,"Obraz snového Havranova",Vector2.zero,Vector2.zero);
        dream.anchorMin=Vector2.zero;dream.anchorMax=Vector2.one;dream.offsetMin=dream.offsetMax=Vector2.zero;
        var backdrop=dream.gameObject.AddComponent<Image>();backdrop.sprite=Resources.Load<Sprite>("MainMenuCastle");backdrop.color=new Color(.45f,.4f,.6f,.45f);backdrop.raycastTarget=false;
        if(backdrop.sprite!=null){var fit=dream.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=backdrop.sprite.rect.width/backdrop.sprite.rect.height;}
        var room=Panel(root,"Ložnice shora",Vector2.zero,new Vector2(1050,580),new Vector2(.5f,.5f));
        room.pivot=Vector2.one*.5f;
        Image Piece(string name,Vector2 at,Vector2 size,Color color)
        {
            var rect=Rect(room,name,Vector2.zero,size);Anchor(rect,Vector2.one*.5f,at);rect.pivot=Vector2.one*.5f;
            var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        Piece("Postel • rám",new Vector2(-170,105),new Vector2(175,280),new Color(.3f,.15f,.07f));
        Piece("Přikrývka",new Vector2(-170,85),new Vector2(155,215),new Color(.16f,.3f,.5f));
        Piece("Polštář",new Vector2(-170,207),new Vector2(130,55),new Color(.8f,.77f,.65f));
        var hero=Piece("Hrdina shora",new Vector2(-340,-160),new Vector2(65,65),new Color(.35f,.55f,.85f));
        var partner=Piece("Partner shora",new Vector2(70,140),new Vector2(65,65),new Color(.7f,.35f,.65f));
        var caption=Label(root,"Po dlouhém dni jdeš spát.",Vector2.zero,new Vector2(1300,110),34,RpgVisualTheme.Cream);
        Anchor(caption.rectTransform,new Vector2(.5f,0),new Vector2(0,100));caption.alignment=TextAnchor.MiddleCenter;
        bool skip=false;
        var skipRect=Rect(root,"Přeskočit úvod",Vector2.zero,new Vector2(270,60));Anchor(skipRect,new Vector2(1,1),new Vector2(-155,-50));
        RpgVisualTheme.Panel(skipRect.gameObject.AddComponent<Image>());
        var button=skipRect.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>skip=true);
        var skipText=Label(skipRect,"Přeskočit úvod",new Vector2(8,-4),new Vector2(254,52),24,RpgVisualTheme.Cream);skipText.alignment=TextAnchor.MiddleCenter;
        float t=0;var start=hero.rectTransform.anchoredPosition;
        while(t<2 && !skip){t+=Time.unscaledDeltaTime;hero.rectTransform.anchoredPosition=Vector2.Lerp(start,new Vector2(-170,130),Mathf.Clamp01(t/2));yield return null;}
        caption.text="Usínáš. Ve snu stojí u postele "+(game.State.heroIsBoy?"tvá partnerka.":"tvůj partner.");
        t=0;while(t<1.8f&&!skip){t+=Time.unscaledDeltaTime;yield return null;}
        caption.text="Známá tvář se mění v monstrum. Zůstaneš tady. Ve snu.";
        t=0;while(t<1.5f&&!skip){t+=Time.unscaledDeltaTime;partner.color=Color.Lerp(new Color(.7f,.35f,.65f),new Color(.6f,.025f,.04f),t/1.5f);partner.rectTransform.localScale=Vector3.one*(1+t/1.5f);yield return null;}
        for(int i=0;i<7;i++)Piece("Mříž snu",new Vector2(-275+i*35,110),new Vector2(9,300),new Color(.1f,.08f,.14f));
        caption.text="Cesta zpět je zamčená. Musíš najít jiný způsob, jak se probudit.";
        t=0;while(t<2.3f&&!skip){t+=Time.unscaledDeltaTime;yield return null;}
        yield return FadeLocation(0,1);
        root.gameObject.SetActive(false);Destroy(root.gameObject);
        yield return FadeLocation(1,0);
        SetGameplayHudVisible(true);
    }
}
