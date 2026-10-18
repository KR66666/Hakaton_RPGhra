using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class RpgInterface : MonoBehaviour
{
    public bool IsModal => modal != null && modal.gameObject.activeSelf;
    private RpgGame game;
    private RectTransform canvas, modal, actions;
    private Text health, stamina, stats, quest, hint, toast, modalTitle, modalBody, combat;
    private Image healthFill, staminaFill, experienceFill;
    private float toastUntil;
    private string currentModal;
    private Font font;
    private bool shopScene;
    private Image arabellaPortrait, merchantPortrait;
    private readonly System.Collections.Generic.List<Sprite> portraitCrops = new System.Collections.Generic.List<Sprite>();
    private RectTransform roomView;
    public bool InRoom => roomView != null;
    private RectTransform mapContent;
    private int travelCompanion;
    private bool travelCart;
    private static readonly Color Gold = new Color(0.91f, 0.76f, 0.43f);
    private static readonly Color Ink = new Color(0.055f, 0.07f, 0.085f, 0.96f);

    public void Initialize(RpgGame owner)
    {
        game = owner; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var obj = new GameObject("RPG Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        obj.transform.SetParent(transform, false);
        canvas = obj.GetComponent<RectTransform>();
        var view = obj.GetComponent<Canvas>(); view.renderMode = RenderMode.ScreenSpaceOverlay; view.sortingOrder = 30;
        var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
        {
            var events = new GameObject("RPG Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(obj.transform, false);
        }

        var status = Panel(canvas, "Player status", new Vector2(20, -20), new Vector2(360, 156), new Vector2(0, 1));
        Label(status, "HAVRANOV • STRÁŽCE VESNICE", new Vector2(18, -12), new Vector2(330, 25), 18, Gold);
        healthFill = Bar(status, new Vector2(18, -48), new Color(0.78f, 0.22f, 0.25f));
        health = Label(status, "", new Vector2(24, -44), new Vector2(310, 24), 17, Color.white);
        staminaFill = Bar(status, new Vector2(18, -80), new Color(0.25f, 0.7f, 0.53f));
        stamina = Label(status, "", new Vector2(24, -76), new Vector2(310, 24), 17, Color.white);
        experienceFill = Bar(status, new Vector2(18, -111), new Color(0.4f, 0.57f, 0.85f));
        stats = Label(status, "", new Vector2(24, -107), new Vector2(320, 38), 15, Color.white);

        var mapButton = Panel(canvas, "Open map", new Vector2(0,-20),new Vector2(180,42),new Vector2(.5f,1));
        var mapClick=mapButton.gameObject.AddComponent<Button>();mapClick.targetGraphic=mapButton.GetComponent<Image>();
        mapClick.onClick.AddListener(ShowWorldMap);
        var mapCaption=Label(mapButton,"MAPA  [M]",new Vector2(0,0),new Vector2(180,42),20,Gold);
        mapCaption.alignment=TextAnchor.MiddleCenter;

        var characterButton=Panel(canvas,"Character upgrades",new Vector2(0,-68),new Vector2(180,38),new Vector2(.5f,1));
        var characterClick=characterButton.gameObject.AddComponent<Button>();characterClick.targetGraphic=characterButton.GetComponent<Image>();
        characterClick.onClick.AddListener(ShowCharacterUpgrades);
        var characterCaption=Label(characterButton,"POSTAVA  [C]",Vector2.zero,new Vector2(180,38),18,Gold);characterCaption.alignment=TextAnchor.MiddleCenter;
        var tracker = Panel(canvas, "Quest tracker", new Vector2(-20, -20), new Vector2(370, 166), new Vector2(1, 1));
        Label(tracker, "DENÍK DOBRODRUHA  [Q]", new Vector2(18, -12), new Vector2(335, 26), 18, Gold);
        quest = Label(tracker, "", new Vector2(18, -44), new Vector2(335, 112), 18, new Color(0.88f, 0.9f, 0.92f));

        var controls = Panel(canvas, "Controls", new Vector2(0, 16), new Vector2(1100, 72), new Vector2(0.5f, 0));
        combat = Label(controls, "", new Vector2(18, -9), new Vector2(1064, 25), 18, Gold);
        var keys = Label(controls, "WASD / šipky  pohyb     Shift  běh     Mezerník  úhyb     E  mluvit / sebrat     I  batoh     Q  deník     Esc  pauza     M  mapa     C  postava", new Vector2(18, -39), new Vector2(1064, 24), 16, Color.white);
        keys.alignment = TextAnchor.MiddleCenter;
        combat.alignment = TextAnchor.MiddleCenter;

        hint = Label(canvas, "", Vector2.zero, new Vector2(680, 45), 23, Gold);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 110)); hint.alignment = TextAnchor.MiddleCenter;
        toast = Label(canvas, "", Vector2.zero, new Vector2(950, 60), 21, Color.white);
        Anchor(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -205)); toast.alignment = TextAnchor.MiddleCenter;
        var shadow = toast.gameObject.AddComponent<Shadow>(); shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -2);

        var shade = Panel(canvas, "Modal shade", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0, 0, 0, 0.68f);
        modal = shade;
        var box = Panel(shade, "Dialog", Vector2.zero, new Vector2(760, 650), new Vector2(0.5f, 0.5f));
        var outline = box.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0.5f, 0.4f, 0.23f); outline.effectDistance = new Vector2(2, 2);
        modalTitle = Label(box, "", new Vector2(32, -22), new Vector2(650, 45), 30, Gold);
        modalBody = Label(box, "", new Vector2(32, -85), new Vector2(696, 290), 22, new Color(0.87f, 0.89f, 0.91f));
        modalBody.alignment = TextAnchor.UpperLeft;
        actions = Rect(box, "Actions", new Vector2(32, -378), new Vector2(696, 245));
        modal.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (game == null || game.State == null) return;
        var state = game.State;
        health.text = $"ZDRAVÍ    {Mathf.CeilToInt(state.health)} / {state.MaxHealth}";
        stamina.text = $"VÝDRŽ    {Mathf.CeilToInt(state.stamina)} / 100";
        stats.text = $"ÚROVEŇ {state.level}    •    {state.experience}/{state.NextLevelExperience} XP    •    {state.gold} zl.";
        healthFill.fillAmount = state.health / state.MaxHealth;
        staminaFill.fillAmount = state.stamina / 100;
        experienceFill.fillAmount = (float)state.experience / state.NextLevelExperience;
        int active = (state.herbsQuest == 1 ? 1 : 0) + (state.suppliesQuest == 1 ? 1 : 0) + (state.deliveryQuest == 1 ? 1 : 0);
        quest.text = QuestSummary() + (active > 0 ? $"\nVedlejší úkoly: {active}  [Q]" : "");
        hint.text = game.NearbyPrompt;
        combat.text = $"[J / levé tlačítko]  {(game.AttackCooldown > 0 ? "ŠVIH…" : "ÚTOK MEČEM")}      [H]  LEKTVAR ×{state.potions}      ÚTOK {state.Damage}      OBRANA {state.Defense}";
        if (Time.unscaledTime >= toastUntil) toast.text = "";
    }

    private string QuestSummary()
    {
        var state = game.State;
        if (state.questStage == 0) return "VESNICE V OHROŽENÍ\nPromluv si s rychtářem Radovanem.\nHledej zlaté jméno na náměstí.";
        if (state.questStage == 2) return "OCHRÁNCE HAVRANOVA\nVesnice je zachráněna.\nProzkoumej mapu a vylepši vybavení.";
        return $"VESNICE V OHROŽENÍ\nNájezdníci  {Mathf.Min(5, state.banditsDefeated)} / 5\nVelitel  {(state.bossDefeated ? "poražen" : state.banditsDefeated >= 5 ? "severně od náměstí" : "odhal jeho úkryt")}\n{(state.CanFinishQuest ? "Vrať se k rychtáři pro odměnu!" : "Útok: J / levé tlačítko myši")}";
    }

    public void Notify(string message, float duration)
    {
        toast.text = message; toastUntil = Time.unscaledTime + duration;
    }

    public void Close()
    {
        if (game.State.health <= 0 && currentModal == "death") return;
        LeaveRoomVisuals();
        game.RestoreOutdoorPlayer();
        modal.gameObject.SetActive(false); currentModal = null;
    }

    public void TogglePause()
    {
        if (game.State.health <= 0) return;
        if (IsModal) { Close(); return; }
        Open("pause", "ODPOČINEK U OHNĚ", "Hra je pozastavena.\n\nPostup se ukládá automaticky každých 30 sekund a po důležitých událostech.\nF5: ručně uložit • F9: načíst uloženou hru.");
        Action("Mapa úrovní  [M]", ShowWorldMap);
        Action("Pokračovat", Close);
        Action("Uložit hru", () => game.Save());
        Action("Hlavní nabídka", game.ReturnToMenu);
        Action("Nový příběh…", ConfirmNewGame);
    }

    public void ToggleInventory()
    {
        if (game.State.health <= 0) return;
        if (currentModal == "inventory") { Close(); return; }
        ShowInventory();
    }

    private void ShowInventory()
    {
        var state = game.State;
        Open("inventory", "BATOH A VYBAVENÍ", $"Zlato: {state.gold}     |     Léčivé lektvary: {state.potions}\nLéčivé byliny: {state.herbs}   •   Zásoby pro vesnici: {state.SuppliesFound}/2\nBalíček pro Petra: {(state.deliveryQuest == 1 && !state.parcelDelivered ? "v batohu" : state.parcelDelivered ? "doručen" : "nepřevzat")}\n\nŽelezný meč +{state.weaponLevel}   •   Útok {state.Damage}\nKožená zbroj +{state.armorLevel}   •   Obrana {state.Defense}\n\nÚroveň {state.level}   •   {state.experience}/{state.NextLevelExperience} XP\nLektvar obnoví 60 zdraví. Vybavení vylepšuje Eliška.");
        Action("Vylepšit postavu  [C]",ShowCharacterUpgrades);
        Action("Vypít lektvar", () => { game.DrinkPotion(); ShowInventory(); });
        Action("Zavřít  [I / Esc]", Close);
    }

    public void ShowCharacterUpgrades()
    {
        if(game.State.health<=0)return;
        var s=game.State;
        Open("character","POSTAVA • VYLEPŠENÍ",$"{(s.heroIsBoy?"Hrdina":"Arabella")} • úroveň {s.level}\nVolné body: {s.UpgradePoints} • jeden bod za každou novou úroveň\n\nVitalita {s.vitalityRank}/5: +15 maximálního zdraví za bod\nSíla {s.strengthRank}/5: +3 poškození za bod\nHbitost {s.agilityRank}/5: +4 % rychlosti za bod\n\nZdraví {s.MaxHealth} • Útok {s.Damage} • Rychlost +{s.agilityRank*4} %\nMeč +{s.weaponLevel} • Zbroj +{s.armorLevel} (vylepšuje obchodnice)");
        for(int i=0;i<3;i++)
        {
            int attribute=i;int rank=i==0?s.vitalityRank:i==1?s.strengthRank:s.agilityRank;
            string name=i==0?"Vitalita":i==1?"Síla":"Hbitost";
            Action(rank>=5?name+" • maximum":name+" • vylepšit za 1 bod",()=>game.UpgradeCharacter(attribute));
            actions.GetChild(actions.childCount-1).GetComponent<Button>().interactable=s.UpgradePoints>0&&rank<5;
        }
        Action("Zavřít",Close);
    }

    public void ToggleJournal()
    {
        if (game.State.health <= 0) return;
        if (currentModal == "journal") { Close(); return; }
        Open("journal", "DENÍK ÚKOLŮ", QuestSummary() + "\n\nVEDLEJŠÍ PŘÍBĚHY\n" + SideSummary(0) + "\n" + SideSummary(1) + "\n" + SideSummary(2) + "\n\nPodrobnosti otevřeš tlačítkem. Úkoly zadávají obyvatelé vesnice.");
        modalBody.fontSize = 18;
        Action("Byliny pro Miru", () => ShowSideQuest(0, false));
        Action("Ztracené zásoby", () => ShowSideQuest(1, false));
        Action("Zásilka pro strážného", () => ShowSideQuest(2, false));
        Action("Zavřít  [Q / Esc]", Close);
    }

    public void ShowMayor()
    {
        var state = game.State;
        if (state.questStage == 0)
        {
            Open("mayor", "RYCHTÁŘ RADOVAN", "Havranov obsadili nájezdníci. Lidé se bojí vyjít ven. Pomůžeš nám?\n\nPoraz alespoň 5 nájezdníků. Potom se ukáže jejich velitel Vranek u severní brány. Zneškodni ho a vrať se sem.\n\nOdměna: 150 zlata, 150 XP a 3 lektvary.");
            Action("Přijmout úkol", game.AcceptQuest);
        }
        else if (state.CanFinishQuest)
        {
            Open("mayor", "RYCHTÁŘ RADOVAN", "Vranek padl? Pak je Havranov konečně svobodný! Přijmi naši vděčnost i slíbenou odměnu.\n\n150 zlata • 150 XP • 3 léčivé lektvary");
            Action("Převzít odměnu", game.ClaimReward);
        }
        else
        {
            Open("mayor", "RYCHTÁŘ RADOVAN", state.questStage == 2 ? "V Havranově pro tebe budou dveře vždy otevřené, ochránce. Prozkoumej vesnici a odpočiň si." : "Drž se, příteli. Nakup si lektvary a nezapomeň na úhyb. Zdraví je cennější než zlato.\n\n" + QuestSummary());
        }
        Action("Úkol: Ztracené zásoby", () => ShowSideQuest(1, true));
        Action("Odejít", Close);
    }

    public void ShowRoomEntrance(string kind, string title)
    {
        if(kind!="merchant") return;
        Open("entrance", "VEJÍT DOVNITŘ?", title + " tě zve dál.\n\nChceš vejít do místnosti a promluvit si?");
        Action("Vejít do místnosti", () => game.BeginRoomEntry(kind));
        Action("Odmítnout • zůstat venku", Close);
    }
    public void EnterRoom(string kind)
    {
        if(kind!="merchant")return;
        BuildRoom(kind);
        if(kind == "merchant") ShowShopDialogue(0); else if(kind == "healer") ShowHealer(); else ShowMayor();
    }
    private void BuildRoom(string kind)
    {
        LeaveRoomVisuals();
        roomView = Rect(canvas,"Interior",Vector2.zero,Vector2.zero);
        roomView.anchorMin=Vector2.zero;roomView.anchorMax=Vector2.one;roomView.offsetMin=roomView.offsetMax=Vector2.zero;
        roomView.SetSiblingIndex(modal.GetSiblingIndex());
        if(kind == "merchant") { BuildShopScene(); return; }
        roomView.gameObject.AddComponent<Image>().color=new Color(.12f,.08f,.055f);
        for(int y=0;y<10;y++) for(int x=0;x<16;x++)
        {
            var plank=Rect(roomView,"Wood plank",new Vector2(x*100,-y*90),new Vector2(98,88));
            var image=plank.gameObject.AddComponent<Image>();image.raycastTarget=false;
            image.color=new Color(.22f+(x+y)%3*.025f,.14f+(x%2)*.015f,.08f);
        }
        for(int shelf=0;shelf<3;shelf++)
        {
            var board=Panel(roomView,"Shelf",new Vector2(55,-85-shelf*125),new Vector2(530,16),new Vector2(0,1));
            board.GetComponent<Image>().color=new Color(.4f,.26f,.12f);
            for(int bottle=0;bottle<8;bottle++)
            {
                var item=Panel(roomView,"Goods",new Vector2(75+bottle*60,-42-shelf*125),new Vector2(24,40),new Vector2(0,1));
                item.GetComponent<Image>().color=bottle%2==0?new Color(.25f,.48f,.3f):new Color(.6f,.38f,.18f);
            }
        }
        var portrait=Rect(roomView,"Speaker portrait",new Vector2(80,-150),new Vector2(390,430));
        var portraitImage=portrait.gameObject.AddComponent<Image>();portraitImage.preserveAspect=true;portraitImage.raycastTarget=false;
        portraitImage.sprite=kind=="merchant"?Resources.Load<Sprite>("ShopWomanPortrait"):MedievalNpcAppearance.Get(kind);
        var counter=Panel(roomView,"Counter",new Vector2(35,-580),new Vector2(550,110),new Vector2(0,1));
        counter.GetComponent<Image>().color=new Color(.25f,.14f,.07f);
        var hero=Rect(roomView,"Your character",new Vector2(220,-650),new Vector2(150,190));
        var heroImage=hero.gameObject.AddComponent<Image>();heroImage.preserveAspect=true;heroImage.raycastTarget=false;
        heroImage.sprite=Resources.Load<Sprite>(game.State.heroIsBoy?"HeroBoyStepA":"HeroUserStepA");
        Label(roomView,"TVOJE POSTAVA",new Vector2(190,-835),new Vector2(230,35),20,Gold);
        modalTitle.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(300,0);
        modal.GetComponent<Image>().color=new Color(0,0,0,.22f);
    }
    private void LeaveRoomVisuals()
    {
        if(roomView!=null) { roomView.gameObject.SetActive(false);Destroy(roomView.gameObject);roomView=null; }
        foreach(var crop in portraitCrops) if(crop!=null) Destroy(crop);
        portraitCrops.Clear();
        shopScene=false; arabellaPortrait=null; merchantPortrait=null;
        if(modalTitle!=null)
        {
            var box=modalTitle.transform.parent.GetComponent<RectTransform>();
            Anchor(box,new Vector2(.5f,.5f),Vector2.zero);box.sizeDelta=new Vector2(760,650);
            modalTitle.rectTransform.anchoredPosition=new Vector2(32,-22);modalTitle.rectTransform.sizeDelta=new Vector2(650,45);modalTitle.fontSize=30;
            modalBody.rectTransform.anchoredPosition=new Vector2(32,-85);modalBody.rectTransform.sizeDelta=new Vector2(696,290);
            actions.anchoredPosition=new Vector2(32,-378);actions.sizeDelta=new Vector2(696,245);
        }
        if(modal!=null) modal.GetComponent<Image>().color=new Color(0,0,0,.68f);
    }

    private void BuildShopScene()
    {
        shopScene=true;
        roomView.gameObject.AddComponent<Image>().color=new Color(.1f,.06f,.03f);
        var backdrop=Rect(roomView,"Shop background",Vector2.zero,Vector2.zero);
        backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.pivot=Vector2.one*.5f;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
        var background=backdrop.gameObject.AddComponent<Image>();background.sprite=Resources.Load<Sprite>("ShopInterior");background.color=Color.white;
        var aspect=backdrop.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
        aspect.aspectRatio=background.sprite!=null?background.sprite.rect.width/background.sprite.rect.height:16f/9f;
        arabellaPortrait=RoomPortrait("Arabella", "ArabellaPortrait",new Vector2(40,246));
        merchantPortrait=RoomPortrait("Obchodnice", "MerchantPortrait",new Vector2(1060,246));
        modal.GetComponent<Image>().color=Color.clear;
    }
    private Image RoomPortrait(string title,string resource,Vector2 position)
    {
        var rect=Rect(roomView,title,Vector2.zero,new Vector2(500,600));
        bool right=resource=="MerchantPortrait";
        rect.anchorMin=new Vector2(right?1:0,0);rect.anchorMax=new Vector2(right?1:0,1);
        rect.offsetMin=new Vector2(right?-540:40,264);rect.offsetMax=new Vector2(right?-40:540,-30);
        var image=rect.gameObject.AddComponent<Image>();
        var original=Resources.Load<Sprite>(resource);
        if(original!=null)
        {
            var crop=original.rect;float trim=resource=="ArabellaPortrait"?.22f:.1f;
            crop.y+=crop.height*trim;crop.height*=1-trim;
            image.sprite=Sprite.Create(original.texture,crop,new Vector2(.5f,0),original.pixelsPerUnit);
            portraitCrops.Add(image.sprite);
        }
        image.preserveAspect=true;image.raycastTarget=false;
        return image;
    }
    private void ShopDialogLayout(string speaker)
    {
        if(!shopScene)return;
        var box=modalTitle.transform.parent.GetComponent<RectTransform>();
        Anchor(box,new Vector2(.5f,0),new Vector2(0,20));box.sizeDelta=new Vector2(1480,244);
        box.GetComponent<Image>().color=new Color(.045f,.075f,.085f,.98f);
        modalTitle.rectTransform.anchoredPosition=new Vector2(28,-14);modalTitle.rectTransform.sizeDelta=new Vector2(990,36);modalTitle.fontSize=26;
        modalBody.rectTransform.anchoredPosition=new Vector2(28,-60);modalBody.rectTransform.sizeDelta=new Vector2(1010,162);modalBody.fontSize=21;
        actions.anchoredPosition=new Vector2(1070,-24);actions.sizeDelta=new Vector2(380,210);
        if(arabellaPortrait!=null) arabellaPortrait.color=speaker=="ARABELLA"?Color.white:new Color(.78f,.78f,.78f);
        if(merchantPortrait!=null) merchantPortrait.color=speaker=="ARABELLA"?new Color(.78f,.78f,.78f):Color.white;
    }
    private void ShowShopDialogue(int line)
    {
        string[] speakers={"OBCHODNICE","ARABELLA","OBCHODNICE","ARABELLA"};
        string[] lines={
            "Vítej v mém obchodě, Arabello. Cesta do Havranova byla jistě dlouhá. Co pro tebe mohu udělat?",
            "Dobrý den. Ve vesnici se mluví o nájezdnících. Potřebuji zásoby a pořádnou výbavu, než vyrazím dál.",
            "Mám léčivé lektvary a mohu ti vylepšit meč i zbroj. A pokud chceš pomoci, čeká tu zásilka pro strážného Petra.",
            "Děkuji. Ukažte mi prosím zboží — a ráda si poslechnu, s čím vám mohu pomoci."};
        line=Mathf.Clamp(line,0,lines.Length-1);
        Open("shopDialogue",speakers[line],lines[line]);ShopDialogLayout(speakers[line]);
        if(line<lines.Length-1) Action("Pokračovat →",()=>ShowShopDialogue(line+1));
        else
        {
            Action("Prohlédnout zboží",ShowMerchant);
            Action("Převzít úkol od obchodnice",()=>ShowSideQuest(2,true));
        }
        Action("Odejít z obchodu",Close);
    }

    public void ShowMerchant()
    {
        var state = game.State;
        Open("merchant", "OBCHODNICE • ZBOŽÍ A VÝBAVA", $"„Dobrý meč a zásoba lektvarů. Víc dobrodruh nepotřebuje.“\n\nTvé zlato: {state.gold}\nLektvary: {state.potions}   •   Meč +{state.weaponLevel}   •   Zbroj +{state.armorLevel}\n\nLektvar: +60 zdraví.\nMeč: +9 poškození za stupeň. Zbroj: −3 zranění za stupeň.");
        Action("Koupit lektvar • 15 zl.", () => { game.BuyPotion(); ShowMerchant(); });
        Action(state.weaponLevel >= 5 ? "Meč • maximální stupeň" : $"Vylepšit meč • {40 + state.weaponLevel * 30} zl.", () => { game.Upgrade(true); ShowMerchant(); });
        Action(state.armorLevel >= 5 ? "Zbroj • maximální stupeň" : $"Vylepšit zbroj • {40 + state.armorLevel * 30} zl.", () => { game.Upgrade(false); ShowMerchant(); });
        Action("Úkol: Zásilka pro strážného", () => ShowSideQuest(2, true));
        Action("Odejít", Close);
    }

    public void ShowHealer()
    {
        var state = game.State;
        Open("healer", "MIRA • BYLINKÁŘKA", $"„S odvahou opatrně. S bylinami ti ale ráda pomohu.“\n\nZdraví: {Mathf.CeilToInt(state.health)} / {state.MaxHealth}\nZlato: {state.gold}   •   Byliny: {state.herbs}   •   Lektvary: {state.potions}\n\nNasbírané byliny přeměním na léčivé lektvary.\nZa 10 zlata tě úplně uzdravím.");
        Action("Vyléčit • 10 zl.", () => { game.Heal(); ShowHealer(); });
        Action("Vyrobit lektvar • 2 byliny", () => { game.CraftPotion(); ShowHealer(); });
        Action("Úkol: Byliny pro Miru", () => ShowSideQuest(0, true));
        Action("Odejít", Close);
    }

    private string SideSummary(int id)
    {
        var state = game.State;
        string name = id == 0 ? "Byliny pro Miru" : id == 1 ? "Ztracené zásoby" : "Zásilka pro strážného";
        int stage = state.SideQuestStage(id);
        if (stage == 2) return "✓ " + name + " • splněno";
        if (stage == 0) return name + " • zadává " + (id == 0 ? "Mira" : id == 1 ? "Radovan" : "Eliška");
        if (state.SideQuestReady(id)) return name + " • vrať se pro odměnu";
        return name + " • " + (id == 0 ? $"byliny {Mathf.Min(state.herbs, 4)}/4" : id == 1 ? $"zásoby {state.SuppliesFound}/2" : "doruč balíček Petrovi");
    }

    public void ShowSideQuest(int id, bool atGiver)
    {
        var state = game.State;
        string title = id == 0 ? "BYLINY PRO MIRU" : id == 1 ? "ZTRACENÉ ZÁSOBY" : "ZÁSILKA PRO STRÁŽNÉHO";
        string story = id == 0
            ? "Mira potřebuje 4 léčivé byliny pro nemocné vesničany. Sbírej zelené značky nebo Miřin záhonek západně od středu náměstí; dorůstá každých 45 sekund.\n\nPři odevzdání spotřebuješ 4 byliny.\nOdměna: 25 zlata, 45 XP a 2 lektvary."
            : id == 1
            ? "Radovan hledá dvě bedny zásob. Jedna zůstala severně od náměstí, druhá na cestě k mostu na východě. Hledej oranžové značky, seber je [E] a vrať se k rychtáři.\n\nOdměna: 60 zlata a 60 XP."
            : "Eliška potřebuje doručit balíček strážnému Petrovi. Stojí na východní cestě u kamenného mostu. Promluv s ním [E] a pak se vrať k Elišce.\n\nOdměna: 40 zlata, 35 XP a 1 lektvar.";
        Open("sideQuest", title, story + "\n\n" + SideSummary(id));
        if (atGiver && state.SideQuestStage(id) == 0)
            Action("Přijmout úkol", () => game.AcceptSideQuest(id));
        else if (atGiver && state.SideQuestReady(id))
            Action("Odevzdat úkol a získat odměnu", () => game.CompleteSideQuest(id));
        else if (!atGiver && state.SideQuestStage(id) != 2)
            Action("Úkol přijmeš a odevzdáš u zadavatele", () => game.Notify("Vyhledej zadavatele ve vesnici."));
        Action(atGiver ? "Zpět k rozhovoru" : "Zpět do deníku", () =>
        {
            if (!atGiver) { currentModal = null; ToggleJournal(); }
            else if (id == 0) ShowHealer();
            else if (id == 1) ShowMayor();
            else ShowMerchant();
        });
        Action("Zavřít", Close);
    }

    public void ShowGuard()
    {
        var state = game.State;
        string message = state.deliveryQuest == 1 && !state.parcelDelivered
            ? "Neseš zásilku od Elišky? Výborně, čekám na ni celé ráno. Předej mi ji a vyřiď jí mé poděkování."
            : state.parcelDelivered ? "Balíček dorazil v pořádku. Eliška na tebe čeká s odměnou." : "Hlídám most, aby se nájezdníci nedostali do vesnice. Pokud chceš pomoci, zeptej se Elišky na zásilku.";
        Open("guard", "STRÁŽNÝ PETR", message);
        if (state.deliveryQuest == 1 && !state.parcelDelivered) Action("Předat balíček", game.DeliverParcel);
        Action("Odejít", Close);
    }

    public void ShowDeath()
    {
        Open("death", "PADL JSI V BOJI", "Nájezdníci tě přemohli. Mira tě může zachránit a přivést zpátky na náměstí.\n\nZtratíš 10 zlata, ale zachováš si zkušenosti, předměty i splněné části úkolu. Přeživší nepřátelé se uzdraví.");
        Action("Vrátit se do vesnice", () => { currentModal = null; game.Respawn(); });
        Action("Hlavní nabídka", game.ReturnToMenu);
    }

    public void ShowVictory()
    {
        var state = game.State;
        Open("victory", "HAVRANOV JE ZACHRÁNĚN", $"Velitel Vranek je poražen a vesnice oslavuje svého ochránce.\n\nOdměna přidána: 150 zlata, 150 XP a 3 lektvary.\n\nDosažená úroveň: {state.level}\nZískané zlato: {state.gold}\n\nMůžeš pokračovat v průzkumu a vylepšování vybavení.");
        Action("Mapa • další úroveň / opakování", ShowWorldMap);
        Action("Pokračovat v dobrodružství", Close);
        Action("Uložit a odejít do nabídky", game.ReturnToMenu);
    }

    public void ShowWorldMap()
    {
        if (game.State.health <= 0) return;
        if (currentModal == "map") { Close(); return; }
        Open("map", "MAPA HAVRANOVA  [M]", "");
        mapContent = Rect(modalBody.transform.parent, "Pixel map", new Vector2(32,-85), new Vector2(696,280));
        var bg = mapContent.gameObject.AddComponent<Image>(); bg.color = new Color(.18f,.28f,.19f);
        // Crisp UI tiles form a pixel map of the existing village and four encounter sectors.
        for (int y=0;y<12;y++) for(int x=0;x<29;x++)
        {
            bool road = y==6 || x==14 || (x>20 && y==5);
            bool water = x==25 && y!=6;
            bool roof = (x%7<3 && y%6<3);
            var tile=Rect(mapContent,"Tile",new Vector2(x*24,-y*22),new Vector2(24,22));
            var image=tile.gameObject.AddComponent<Image>(); image.raycastTarget=false;
            image.color=water ? new Color(.22f,.43f,.53f) : road ? new Color(.62f,.55f,.4f) : roof ? new Color(.4f,.23f,.15f) : new Color(.21f+(x+y)%3*.02f,.31f,.19f);
        }
        Vector2[] points={new Vector2(348,145),new Vector2(348,50),new Vector2(565,145),new Vector2(155,112)};
        for(int i=0;i<RpgCampaign.Names.Length;i++)
        {
            int area=i; bool unlocked=game.State.AreaUnlocked(i), done=game.State.completedAreas.Contains(i);
            var node=Rect(mapContent,"Level "+i,new Vector2(points[i].x-68,-points[i].y),new Vector2(136,42));
            var image=node.gameObject.AddComponent<Image>();
            image.color=!unlocked ? new Color(.18f,.18f,.18f) : done ? new Color(.15f,.4f,.25f) : new Color(.6f,.4f,.12f);
            var button=node.gameObject.AddComponent<Button>();button.targetGraphic=image;
            button.onClick.AddListener(()=>ShowTravel(area,0,false));
            var caption=Label(node,(done?"✓ ":unlocked?"":"ZÁMEK ")+(i+1)+". "+RpgCampaign.Names[i],new Vector2(3,-2),new Vector2(130,40),14,Color.white);
            caption.alignment=TextAnchor.MiddleCenter;
            if(i==game.State.currentArea)
            {
                var pin=Rect(node,"Current sector",new Vector2(58,12),new Vector2(20,12));
                pin.gameObject.AddComponent<Image>().color=Gold;
            }
        }
        Vector3 pos=game.Player.position;
        var dot=Rect(mapContent,"Player position",new Vector2(Mathf.Clamp01((pos.x+20)/40)*680,-Mathf.Clamp01((15.333f-pos.y)/26.666f)*250),new Vector2(10,10));
        dot.gameObject.AddComponent<Image>().color=Color.cyan;
        Action("Modrá tečka = tvoje poloha • zlatá značka = aktuální úroveň",()=>{});
        Action("Klikni na úroveň • zelená = splněná",()=>{});
        Action("Zavřít mapu",Close);
    }
    private void ShowTravel(int area, int companion, bool cart)
    {
        travelCompanion=companion;travelCart=cart;
        var state=game.State;
        bool unlocked=state.AreaUnlocked(area),done=state.completedAreas.Contains(area);
        string problem=state.TravelProblem(area,companion,cart);
        string who=companion==0?"Sám / sama":companion==1?"Strážný Petr • pomáhá v boji":"Mira • léčí během cesty";
        Open("travel",RpgCampaign.Names[area],
            (done?"DOKONČENO • můžeš projít znovu":unlocked?"DOSTUPNÁ ÚROVEŇ":"ZAMČENO")+
            "\n\nPodmínka: "+(area==0?"dostupné od začátku":"dokončit "+RpgCampaign.Names[area-1])+
            "\nCesta: "+(cart?"vozem • 10 zlata":"pěšky • zdarma")+"\nDoprovod: "+who+
            "\n\nPetr: dokončit Zásilku pro strážného. Mira: dokončit Byliny pro Miru."+
            "\n"+(problem??"Všechny podmínky splněné.")+
            "\n\nČtyři bojové úrovně ve stejné vesnici. Cíl: porazit 5 nájezdníků a velitele, odměnu převzít u Radovana.");
        modalBody.fontSize=18;
        if(unlocked)
        {
            Action("Doprovod: "+who+" • změnit",()=>ShowTravel(area,(companion+1)%3,cart));
            Action("Způsob cesty: "+(cart?"vůz":"pěšky")+" • změnit",()=>ShowTravel(area,companion,!cart));
            if(problem==null)
            {
                if(area==state.currentArea && !done) Action("Pokračovat v této úrovni",Close);
                else Action(done?"Projít úroveň znovu":"Vyrazit do úrovně",()=>game.TravelToArea(area,companion,cart,done));
            }
        }
        Action("Zpět na mapu",ShowWorldMap);
    }

    private void ConfirmNewGame()
    {
        Open("new", "ZAČÍT NOVÝ PŘÍBĚH?", "Nový příběh nahradí současnou uloženou hru. Začneš znovu na náměstí s počáteční výbavou.");
        Action("Ano, začít nový příběh", game.NewGame);
        Action("Zpět", () => { Close(); TogglePause(); });
    }

    private void Open(string kind, string title, string body)
    {
        if (InRoom && (kind == "map" || kind == "pause" || kind == "death" || kind == "travel" || kind == "character")) LeaveRoomVisuals();
        if (mapContent != null) { mapContent.gameObject.SetActive(false); Destroy(mapContent.gameObject); mapContent = null; }
        currentModal = kind;
        modalBody.fontSize = 22;
        modalTitle.text = title; modalBody.text = body;
        for (int i = actions.childCount - 1; i >= 0; i--)
        {
            var child = actions.GetChild(i); child.gameObject.SetActive(false); child.SetParent(null); Destroy(child.gameObject);
        }
        modal.gameObject.SetActive(true);
        if(shopScene) ShopDialogLayout(kind=="shopDialogue"?title:"OBCHODNICE");
    }

    private void Action(string title, Action callback)
    {
        int index = actions.childCount;
        float width=shopScene?380:696, height=shopScene?34:42, spacing=shopScene?42:50;
        var rect = Rect(actions, title, new Vector2(0, -index * spacing), new Vector2(width, height));
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.16f, 0.21f, 0.23f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1, 0.88f, 0.6f); colors.pressedColor = new Color(0.6f, 0.8f, 0.7f); button.colors = colors;
        button.onClick.AddListener(() => callback());
        var text = Label(rect, title, new Vector2(12, 0), new Vector2(width-24, height), shopScene?18:21, Gold); text.alignment = TextAnchor.MiddleCenter;
    }

    private Image Bar(Transform parent, Vector2 position, Color color)
    {
        var track = Panel(parent, "Bar", position, new Vector2(324, 23), new Vector2(0, 1));
        track.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.16f);
        var fillRect = Rect(track, "Fill", Vector2.zero, new Vector2(324, 23));
        var image = fillRect.gameObject.AddComponent<Image>(); image.color = color; image.sprite = RpgGame.MarkerSprite();
        // A white one-pixel texture makes a rectangular filled bar.
        image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f);
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.raycastTarget = false;
        return image;
    }

    private RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var rect = Rect(parent, name, Vector2.zero, size); Anchor(rect, anchor, position);
        var image = rect.gameObject.AddComponent<Image>(); image.color = Ink;
        return rect;
    }

    private Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var rect = Rect(parent, text, position, size);
        var label = rect.gameObject.AddComponent<Text>(); label.text = text; label.font = font;
        label.fontSize = fontSize; label.color = color; label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position;
    }
}
