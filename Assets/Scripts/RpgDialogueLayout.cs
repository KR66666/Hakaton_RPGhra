using UnityEngine;

// One geometry for all conversations, regardless of speaker or backdrop.
public static class RpgDialogueLayout
{
    public static readonly Vector2 Size=new Vector2(1480,540);
    public static readonly Rect Portrait=new Rect(24,22,400,496);
    public static readonly Rect Title=new Rect(448,22,1008,44);
    public static readonly Rect Body=new Rect(448,84,1008,210);
    public static readonly Rect Actions=new Rect(448,314,1008,204);
    public const float ButtonWidth=492,ButtonHeight=60;
    public static Vector2 ButtonPosition(int index) => new Vector2(index%2*516,-index/2*72);
    public static bool IsConversation(string kind)
    {
        switch(kind)
        {
            case "guideInstruction":case "cellarGuide":case "guard":case "mayor":case "healer":
            case "merchant":case "shopDialogue":case "heroShopDialogue":case "resident":case "entrance":case "sideQuest":
            case "cellarElder":case "cellarMerchant":case "cellarWalk":case "cellarRats":
            case "cellarEscape":case "cellarNote":case "cellarClue":return true;
            default:return false;
        }
    }
}

public sealed partial class RpgInterface
{
    private bool conversationActive;
    private RectTransform conversationPortrait,conversationViewport,conversationScrollBar;
    private UnityEngine.UI.ContentSizeFitter conversationTextFit;

    private static void PlaceDialogueRect(RectTransform rect,Rect layout)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(layout.x,-layout.y);rect.sizeDelta=layout.size;
    }
    private void ResetConversationLayout()
    {
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        if(conversationTextFit!=null)conversationTextFit.enabled=false;
        modalBody.transform.SetParent(box,false);
        foreach(var item in new[]{conversationPortrait,conversationViewport,conversationScrollBar})
            if(item!=null){item.gameObject.SetActive(false);Destroy(item.gameObject);}
        conversationPortrait=conversationViewport=conversationScrollBar=null;conversationActive=false;
        if(phoneChrome!=null){phoneChrome.gameObject.SetActive(false);Destroy(phoneChrome.gameObject);phoneChrome=null;}
        modalTitle.resizeTextForBestFit=false;modalTitle.color=Gold;modalBody.color=new Color(.87f,.89f,.91f);
        Anchor(box,new Vector2(.5f,.5f),Vector2.zero);box.sizeDelta=new Vector2(900,760);
        RpgVisualTheme.Panel(box.GetComponent<UnityEngine.UI.Image>());
        var outline=box.GetComponent<UnityEngine.UI.Outline>();if(outline!=null){outline.enabled=true;outline.effectColor=new Color(.5f,.4f,.23f);outline.effectDistance=new Vector2(2,2);}
        PlaceDialogueRect(modalTitle.rectTransform,new Rect(32,22,836,55));modalTitle.fontSize=38;
        PlaceDialogueRect(modalBody.rectTransform,new Rect(32,94,836,280));modalBody.fontSize=28;modalBody.lineSpacing=1;
        modalBody.resizeTextForBestFit=false;modalBody.alignment=TextAnchor.UpperLeft;
        PlaceDialogueRect(actions,new Rect(32,394,836,344));
        modal.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.4f);
    }
    private void ApplyConversationLayout(string kind,string speaker)
    {
        conversationActive=true;
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        Anchor(box,new Vector2(.5f,0),new Vector2(0,22));box.sizeDelta=RpgDialogueLayout.Size;
        bool fullPetr=kind=="guideInstruction"||kind=="cellarGuide"||kind=="guard";
        var portraitArea=RpgDialogueLayout.Portrait;
        RpgVisualTheme.Panel(box.GetComponent<UnityEngine.UI.Image>());
        var outline=box.GetComponent<UnityEngine.UI.Outline>();if(outline!=null)outline.enabled=false;
        modal.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,InRoom?.18f:.38f);
        conversationPortrait=Panel(box,"Portrét mluvčího",Vector2.zero,Vector2.zero,new Vector2(0,1));
        PlaceDialogueRect(conversationPortrait,portraitArea);
        conversationPortrait.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var art=Rect(conversationPortrait,"Mluvčí",Vector2.zero,Vector2.zero);
        art.anchorMin=Vector2.zero;art.anchorMax=Vector2.one;art.pivot=new Vector2(.5f,0);
        art.offsetMin=new Vector2(8,0);art.offsetMax=new Vector2(-8,-8);
        var image=art.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=RpgPortraitFit.VisibleSprite(ConversationPortrait(kind,speaker));image.preserveAspect=true;image.raycastTarget=false;
        // Use the wider portrait column for every speaker, with a close waist-up crop.
        art.localScale=new Vector3(1.1f,1.1f,1);
        art.offsetMin=new Vector2(8,-40);art.offsetMax=new Vector2(-8,-48);
        if(fullPetr)
        {
            art.localScale=new Vector3(1.35f,1.35f,1);
            art.offsetMin=new Vector2(8,-60);art.offsetMax=new Vector2(-8,-68);
        }
        // Seat the portrait behind a small wooden lip: the source's cut waist
        // meets the frame instead of floating visibly above its lower edge.
        var lip=Rect(conversationPortrait,"Spodní hrana rámu",Vector2.zero,new Vector2(portraitArea.width-4,7));
        Anchor(lip,new Vector2(.5f,0),Vector2.zero);
        lip.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.20f,.14f,.085f);
        PlaceDialogueRect(modalTitle.rectTransform,RpgDialogueLayout.Title);modalTitle.fontSize=34;
        PlaceDialogueRect(actions,RpgDialogueLayout.Actions);
        ApplyScrollableBody(RpgDialogueLayout.Body,28);
    }
    private void ApplyScrollableBody(Rect textArea,int fontSize)
    {
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        float scrollbarX=textArea.xMax-8;
        textArea.width-=16;
        conversationViewport=Rect(box,"Text rozhovoru",Vector2.zero,Vector2.zero);PlaceDialogueRect(conversationViewport,textArea);
        conversationViewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.clear;
        conversationViewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var scroll=conversationViewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.viewport=conversationViewport;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=28;
        scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
        modalBody.transform.SetParent(conversationViewport,false);
        var body=modalBody.rectTransform;body.anchorMin=new Vector2(0,1);body.anchorMax=new Vector2(1,1);body.pivot=new Vector2(0,1);
        body.anchoredPosition=Vector2.zero;body.sizeDelta=new Vector2(0,textArea.height);
        modalBody.fontSize=fontSize;modalBody.lineSpacing=1.15f;modalBody.resizeTextForBestFit=false;modalBody.supportRichText=true;
        scroll.content=body;
        conversationTextFit=modalBody.GetComponent<UnityEngine.UI.ContentSizeFitter>()??modalBody.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        conversationTextFit.enabled=true;conversationTextFit.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        conversationTextFit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        conversationScrollBar=Rect(box,"Posuvník textu",Vector2.zero,Vector2.zero);
        PlaceDialogueRect(conversationScrollBar,new Rect(scrollbarX,textArea.y,8,textArea.height));
        conversationScrollBar.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.12f,.08f,.05f);
        var bar=conversationScrollBar.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        var handle=Rect(conversationScrollBar,"Úchyt",Vector2.zero,Vector2.zero);
        handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
        var handleImage=handle.gameObject.AddComponent<UnityEngine.UI.Image>();handleImage.color=Gold;bar.handleRect=handle;bar.targetGraphic=handleImage;
        scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.verticalNormalizedPosition=1;
    }
    private Sprite ConversationPortrait(string kind,string speaker)
    {
        if(kind=="guideInstruction"||kind=="cellarGuide"||kind=="guard")return GuidePortrait();
        if(kind=="heroShopDialogue")return game.State.heroIsBoy?game.HeroSprite:RpgPortraitFit.ArabellaShop();
        if(kind=="entrance")return RpgVisualTheme.Icon("entrance");
        if(kind=="merchant"||kind=="shopDialogue"||kind=="cellarMerchant")return Resources.Load<Sprite>("MerchantPortrait");
        if(kind=="mayor")return MedievalNpcAppearance.Get("mayor");
        if(kind=="healer")return MedievalNpcAppearance.Get("healer");
        if(kind=="cellarElder")return Resources.Load<Sprite>("MatejPortraitCutout");
        if(kind=="sideQuest")return speaker.Contains("MIRU")?MedievalNpcAppearance.Get("healer"):
            speaker.Contains("ZÁSOBY")?MedievalNpcAppearance.Get("mayor"):Resources.Load<Sprite>("MerchantPortrait");
        if(kind.StartsWith("cellar"))return game.State.heroIsBoy?game.HeroSprite:RpgPortraitFit.ArabellaShop();
        return MedievalNpcAppearance.Get("villager");
    }
}
