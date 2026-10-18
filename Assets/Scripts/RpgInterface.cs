using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed partial class RpgInterface : MonoBehaviour
{
    public bool IsModal => modal != null && modal.gameObject.activeSelf;
    private RpgGame game;
    private RectTransform canvas, modal, actions;
    private Text health, stamina, stats, quest, hint, toast, modalTitle, modalBody, combat;
    private Text locationTitle;
    private Image healthFill, staminaFill, experienceFill;
    private float toastUntil;
    private string currentModal;
    private Font font;
    private RectTransform miniMap, miniPlayer, miniGoal, goalArrow;
    private Text goalHint;
    private Sprite directionSprite;
    private RectTransform goalPanel, interactionPanel;
    private Image miniImage;
    private CanvasGroup gameplayHud;
    private readonly System.Collections.Generic.List<RectTransform> enemyDots = new System.Collections.Generic.List<RectTransform>();
    private RectTransform roomView;
    public bool InRoom => roomView != null;
    private RectTransform mapContent;
    private int travelCompanion;
    private bool travelCart;
    private static readonly Color Gold = RpgVisualTheme.Gold;
    private static readonly Color Ink = new Color(0.055f, 0.07f, 0.085f, 0.96f);

    public void Initialize(RpgGame owner)
    {
        game = owner; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var obj = new GameObject("RPG Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        // The HUD must not inherit the character's heading, scale or walking motion.
        obj.transform.SetParent(null, false);
        canvas = obj.GetComponent<RectTransform>();
        var view = obj.GetComponent<Canvas>(); view.renderMode = RenderMode.ScreenSpaceOverlay; view.sortingOrder = 30;view.pixelPerfect=false;
        var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        if (EventSystem.current == null)
        {
            var events = new GameObject("RPG Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(obj.transform, false);
        }

        var status = Panel(canvas, "Player status", new Vector2(20, -20), new Vector2(360, 188), new Vector2(0, 1));
        locationTitle=Label(status, game.IsTestLocation?"TESTOVACÍ LOKACE  [T]":game.LocationName.ToUpperInvariant(), new Vector2(18, -12), new Vector2(324, 27), 20, Gold);
        locationTitle.fontStyle=FontStyle.Bold;
        locationTitle.resizeTextForBestFit=true;locationTitle.resizeTextMinSize=16;locationTitle.resizeTextMaxSize=20;
        health = Label(status, "", new Vector2(18, -44), new Vector2(324, 24), 18, RpgVisualTheme.Cream);
        healthFill = Bar(status, new Vector2(18, -73), new Color(.57f,.17f,.14f));
        stamina = Label(status, "", new Vector2(18, -95), new Vector2(324, 24), 18, RpgVisualTheme.Cream);
        staminaFill = Bar(status, new Vector2(18, -124), new Color(.40f,.46f,.23f));
        stats = Label(status, "", new Vector2(18, -145), new Vector2(324, 25), 16, RpgVisualTheme.Cream);
        stats.resizeTextForBestFit=true;stats.resizeTextMinSize=13;stats.resizeTextMaxSize=16;
        experienceFill = Bar(status, new Vector2(18, -173), new Color(.65f,.51f,.28f), 5);

        var mapButton = Panel(canvas, "Open map", new Vector2(-86,-20),new Vector2(164,42),new Vector2(.5f,1));
        var mapClick=mapButton.gameObject.AddComponent<Button>();mapClick.targetGraphic=mapButton.GetComponent<Image>();
        mapClick.onClick.AddListener(ShowWorldMap);
        var mapCaption=Label(mapButton,"MAPA  [M]",new Vector2(0,0),new Vector2(164,42),19,Gold);
        mapCaption.alignment=TextAnchor.MiddleCenter;

        var characterButton=Panel(canvas,"Character upgrades",new Vector2(86,-20),new Vector2(164,42),new Vector2(.5f,1));
        var characterClick=characterButton.gameObject.AddComponent<Button>();characterClick.targetGraphic=characterButton.GetComponent<Image>();
        characterClick.onClick.AddListener(ShowCharacterUpgrades);
        var characterCaption=Label(characterButton,"POSTAVA  [C]",Vector2.zero,new Vector2(164,42),19,Gold);characterCaption.alignment=TextAnchor.MiddleCenter;
        if(game.IsTestLocation)
        {
            var tools=Panel(canvas,"Test tools",new Vector2(20,102),new Vector2(240,42),new Vector2(0,0));
            var toolsClick=tools.gameObject.AddComponent<Button>();toolsClick.targetGraphic=tools.GetComponent<Image>();toolsClick.onClick.AddListener(ShowTestTools);
            var toolsLabel=Label(tools,"TESTOVACÍ NÁSTROJE  [T]",Vector2.zero,new Vector2(240,42),16,Gold);toolsLabel.alignment=TextAnchor.MiddleCenter;
        }
        var tracker = Panel(canvas, "Quest tracker", new Vector2(-20, -240), new Vector2(320, 210), new Vector2(1, 1));
        tracker.GetComponent<Image>().sprite=RpgVisualTheme.Parchment;
        var journalTitle=Label(tracker, "CESTOVNÍ DENÍK  [Q]", new Vector2(18, -16), new Vector2(284, 28), 19, new Color(.25f,.16f,.09f));journalTitle.fontStyle=FontStyle.Bold;
        quest = Label(tracker, "", new Vector2(18, -53), new Vector2(284, 140), 18, new Color(.22f,.15f,.10f));
        quest.lineSpacing=1.1f;

        var miniBox=Panel(canvas,"Mini map",new Vector2(-20,-20),new Vector2(250,202),new Vector2(1,1));
        Label(miniBox,"OKOLÍ • MAPA [M]",new Vector2(10,-172),new Vector2(230,25),16,Gold);
        miniMap=Rect(miniBox,"Village overview",new Vector2(10,-10),new Vector2(230,153));
        miniImage=miniMap.gameObject.AddComponent<Image>();miniImage.sprite=game.MapSprite;miniImage.raycastTarget=false;miniImage.color=Color.white;
        var miniClick=miniBox.gameObject.AddComponent<Button>();miniClick.targetGraphic=miniBox.GetComponent<Image>();miniClick.onClick.AddListener(ShowWorldMap);
        for(int i=0;i<8;i++)enemyDots.Add(MapDot("Enemy",new Color(.95f,.25f,.2f),5));
        miniGoal=MapDot("Quest goal",Gold,9);miniPlayer=MapDot("Player",Color.cyan,7);
        goalPanel=Panel(canvas,"Tracked destination",new Vector2(0,-76),new Vector2(400,64),new Vector2(.5f,1));
        goalHint=Label(goalPanel,"",new Vector2(42,-10),new Vector2(338,44),17,RpgVisualTheme.Cream);goalHint.alignment=TextAnchor.MiddleLeft;
        goalArrow=Rect(goalPanel,"Goal direction",new Vector2(17,-24),new Vector2(15,15));goalArrow.pivot=Vector2.one*.5f;
        var arrowImage=goalArrow.gameObject.AddComponent<Image>();arrowImage.sprite=DirectionSprite();arrowImage.color=Gold;arrowImage.raycastTarget=false;

        var controls = Panel(canvas, "Controls", new Vector2(0, 16), new Vector2(1000, 62), new Vector2(0.5f, 0));
        combat = Label(controls, "", new Vector2(18, -8), new Vector2(964, 23), 17, Gold);
        var keys = Label(controls, "WASD  pohyb   •   Shift  běh   •   Mezerník  úhyb   •   I  batoh   •   Q  deník   •   M  mapa   •   C  postava   •   Esc  pauza", new Vector2(18, -33), new Vector2(964, 23), 16, RpgVisualTheme.Cream);
        keys.alignment = TextAnchor.MiddleCenter;
        keys.resizeTextForBestFit=true;keys.resizeTextMinSize=14;keys.resizeTextMaxSize=16;
        combat.alignment = TextAnchor.MiddleCenter;

        interactionPanel=Panel(canvas,"Interaction prompt",new Vector2(0,94),new Vector2(600,42),new Vector2(.5f,0));
        hint = Label(interactionPanel, "", new Vector2(12,-5), new Vector2(576,32), 21, RpgVisualTheme.Cream);hint.alignment = TextAnchor.MiddleCenter;
        toast = Label(canvas, "", Vector2.zero, new Vector2(680, 64), 20, RpgVisualTheme.Cream);
        Anchor(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -154)); toast.alignment = TextAnchor.MiddleCenter;
        var shadow = toast.gameObject.AddComponent<Shadow>(); shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -2);

        var shade = Panel(canvas, "Modal shade", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().sprite=null;shade.GetComponent<Image>().color = new Color(0, 0, 0, 0.40f);
        modal = shade;
        var box = Panel(shade, "Dialog", Vector2.zero, new Vector2(760, 650), new Vector2(0.5f, 0.5f));
        var outline = box.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0.5f, 0.4f, 0.23f); outline.effectDistance = new Vector2(2, 2);
        modalTitle = Label(box, "", new Vector2(32, -22), new Vector2(650, 45), 30, Gold);
        modalBody = Label(box, "", new Vector2(32, -85), new Vector2(696, 290), 22, new Color(0.87f, 0.89f, 0.91f));
        modalBody.alignment = TextAnchor.UpperLeft;
        actions = Rect(box, "Actions", new Vector2(32, -378), new Vector2(696, 245));
        modal.gameObject.SetActive(false);
        BuildThemedHud();
        // Put game widgets in their own layer so a conversation can hide the HUD
        // without disabling input, the modal, or the transition fade.
        var hudRoot=Rect(canvas,"Gameplay HUD",Vector2.zero,Vector2.zero);
        hudRoot.anchorMin=Vector2.zero;hudRoot.anchorMax=Vector2.one;hudRoot.offsetMin=hudRoot.offsetMax=Vector2.zero;
        gameplayHud=hudRoot.gameObject.AddComponent<CanvasGroup>();
        var widgets=new System.Collections.Generic.List<Transform>();
        foreach(Transform child in canvas)
            if(child!=hudRoot&&child!=modal&&child.GetComponent<EventSystem>()==null)widgets.Add(child);
        foreach(var widget in widgets)widget.SetParent(hudRoot,false);
        hudRoot.SetAsFirstSibling();
    }

    private void Update()
    {
        if (game == null || game.State == null) return;
        var state = game.State;
        locationTitle.text=game.IsTestLocation?"TESTOVACÍ LOKACE [T]":game.LocationName.ToUpperInvariant();
        UpdateThemedHud();
        health.text = $"ZDRAVÍ    {Mathf.CeilToInt(state.health)} / {state.MaxHealth}";
        stamina.text = $"VÝDRŽ    {Mathf.CeilToInt(state.stamina)} / 100";
        stats.text = $"ÚROVEŇ {state.level}    •    {state.experience}/{state.NextLevelExperience} XP    •    {state.gold} zl.";
        healthFill.fillAmount = state.health / state.MaxHealth;
        staminaFill.fillAmount = state.stamina / 100;
        experienceFill.fillAmount = (float)state.experience / state.NextLevelExperience;
        int active = (state.herbsQuest == 1 ? 1 : 0) + (state.suppliesQuest == 1 ? 1 : 0) + (state.deliveryQuest == 1 ? 1 : 0);
        quest.text = QuestSummary() + (active > 0 ? $"\nVedlejší úkoly: {active}  [Q]" : "");
        hint.text = game.NearbyPrompt;
        interactionPanel.gameObject.SetActive(!string.IsNullOrEmpty(hint.text)&&!IsModal);
        combat.text = $"J  útok     •     H  lektvar ×{state.potions}     •     E  rozhovor / předmět";
        UpdateMiniMap();
        if(state.trackedQuest>=0 && state.SideQuestStage(state.trackedQuest)==1) quest.text=SideSummary(state.trackedQuest)+"\nSledovaný úkol • Q pro změnu";
        if(game.IsTestLocation) quest.text="TESTOVACÍ VERZE\nNPC a výbava jsou obnovitelné.\nT • nástroje / F9 • obnovit\nHlavní příběh se nepřepisuje.";
        else if(!game.InTown)quest.text=game.LocationName.ToUpperInvariant()+"\n"+RpgLocations.Description(state.place);
        if (Time.unscaledTime >= toastUntil) toast.text = "";
    }

    private Sprite DirectionSprite()
    {
        var texture=new Texture2D(14,14,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=new Color32[196];
        for(int y=0;y<14;y++)for(int x=0;x<14;x++)
            if(y>=6&&Mathf.Abs(x-6.5f)<=13-y || y<7&&x>=5&&x<=8) pixels[y*14+x]=new Color32(255,255,255,255);
        texture.SetPixels32(pixels);texture.Apply();
        directionSprite=Sprite.Create(texture,new Rect(0,0,14,14),Vector2.one*.5f,14);return directionSprite;
    }
    private void OnDestroy()
    {
        if(directionSprite!=null){Destroy(directionSprite.texture);Destroy(directionSprite);}
        if(canvas!=null)Destroy(canvas.gameObject);
    }

    private RectTransform MapDot(string name,Color color,float size)
    {
        var rect=Rect(miniMap,name,Vector2.zero,new Vector2(size,size));rect.pivot=Vector2.one*.5f;
        var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
    }
    private void LocateDot(RectTransform dot,Vector3 position)
    {
        var bounds=game.MapBounds;
        dot.anchoredPosition=new Vector2(Mathf.Clamp01((position.x-bounds.min.x)/bounds.size.x)*230,-Mathf.Clamp01((bounds.max.y-position.y)/bounds.size.y)*153);
    }
    private void UpdateMiniMap()
    {
        if(miniImage.sprite!=game.MapSprite)miniImage.sprite=game.MapSprite;
        LocateDot(miniPlayer,game.Player.position);
        int index=0;
        foreach(var enemy in game.Enemies)
        {
            if(enemy==null||enemy.Dead||index>=enemyDots.Count)continue;
            enemyDots[index].gameObject.SetActive(true);LocateDot(enemyDots[index++],enemy.transform.position);
        }
        for(;index<enemyDots.Count;index++)enemyDots[index].gameObject.SetActive(false);
        bool hasGoal=game.TryTrackedGoal(out var target,out var title);
        goalPanel.gameObject.SetActive(hasGoal&&!IsModal);
        miniGoal.gameObject.SetActive(hasGoal);goalArrow.gameObject.SetActive(hasGoal);
        goalHint.text=hasGoal?title+" • "+Mathf.CeilToInt(Vector2.Distance(game.Player.position,target))+" m":"Žádný sledovaný cíl • otevři deník Q";
        if(hasGoal)
        {
            LocateDot(miniGoal,target);
            Vector2 delta=target-game.Player.position;
            goalArrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);
        }
    }

    private string QuestSummary()
    {
        if(game.State.cellarQuest > 0 && game.State.cellarQuest < 7) return "TAJEMSTVÍ SKLEPA\n" + game.State.CellarObjective;
        var state = game.State;
        if(state.currentArea==0) return "POKOJNÝ HAVRANOV\n"+(state.cellarQuest==7?"Vesnický příběh dokončen. Další cesta: [M].":state.CellarObjective);
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
        if(cellarAnimating) return;
        ResetGuideDialogue();
        if (game.State.health <= 0 && currentModal == "death") return;
        LeaveRoomVisuals();
        game.RestoreOutdoorPlayer();
        modal.gameObject.SetActive(false); currentModal = null;
        SetGameplayHudVisible(true);
    }

    public void TogglePause()
    {
        if(cellarAnimating) return;
        if(game.IsTestLocation){if(IsModal)Close();else ShowTestTools();return;}
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

    public void ShowTestTools()
    {
        if(!game.IsTestLocation)return;
        Open("testTools","TESTOVACÍ LOKACE", "NPC nahoře • předměty uprostřed • výbava dole • nepřátelé na severu.\n\n[E] rozhovor / vyzkoušet předmět • [T] testovací nástroje\n\nMeče i zbroje +0 až +5 lze nasadit bez placení. Vylepšení postavy je v C.\n\nHráč je nezranitelný. Test nikdy nečte ani nepřepisuje uložený příběh.");
        Action("Obnovit všechna NPC a předměty",game.ResetTestLocation);
        Action(game.State.questStage==1?"Vypnout testovací souboj":"Zapnout testovací souboj",game.ToggleTestCombat);
        Action("Hlavní menu",game.ReturnToMenu);
        Action("Zpět do testovací lokace",Close);
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
        Open("journal", "DENÍK ÚKOLŮ", QuestSummary() + "\n\nVEDLEJŠÍ PŘÍBĚHY\n" + SideSummary(0) + "\n" + SideSummary(1) + "\n" + SideSummary(2) + "\n\nODMĚNY (zlato / XP): hlavní 150/150 • byliny 25/45 • zásoby 60/60 • zásilka 40/35.\nPodrobnosti a sledování otevřeš tlačítkem.");
        modalBody.fontSize = 18;
        Action("Tajemství sklepa • deník a lístek", ShowCellarJournal);
        Action("Sledovat hlavní úkol",()=>game.TrackQuest(-1));
        Action("Byliny pro Miru", () => ShowSideQuest(0, false));
        Action("Ztracené zásoby", () => ShowSideQuest(1, false));
        Action("Zásilka pro strážného", () => ShowSideQuest(2, false));
        Action("Zavřít  [Q / Esc]", Close);
    }

    public void ShowMayor()
    {
        var state = game.State;
        if(state.currentArea==0 && !game.IsTestLocation)
        {
            Open("mayor", "RYCHTÁŘ RADOVAN", "V Havranově žijeme pokojně. Lidé tu pracují, obchodují a starají se o své domy. Petr Kříž tě provede vesnicí. Zeptej se ho na cestu a na svíčky pro výpravu.\n\n"+state.CellarObjective);
            Action("Úkol: Ztracené zásoby",()=>ShowSideQuest(1,true));
            Action("Odejít",Close);
            return;
        }
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
        if(kind == "merchant" && game.State.cellarQuest >= 2 && game.State.cellarQuest < 6) ShowCellarMerchant();
        else if(kind == "merchant") ShowShopDialogue(0); else if(kind == "healer") ShowHealer(); else ShowMayor();
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
    }
    private void LeaveRoomVisuals()
    {
        ResetConversationLayout();
        if(roomView!=null) {roomView.SetParent(null);roomView.gameObject.SetActive(false);Destroy(roomView.gameObject);roomView=null;}
    }
    private void BuildShopScene()
    {
        roomView.gameObject.AddComponent<Image>().color=new Color(.1f,.06f,.03f);
        var backdrop=Rect(roomView,"Shop background",Vector2.zero,Vector2.zero);
        backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.pivot=Vector2.one*.5f;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
        var background=backdrop.gameObject.AddComponent<Image>();background.sprite=Resources.Load<Sprite>("ShopInterior");background.color=Color.white;RpgVisualTheme.Picture(background,false,3);
        var aspect=backdrop.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
        aspect.aspectRatio=background.sprite!=null?background.sprite.rect.width/background.sprite.rect.height:16f/9f;
    }
    private void ShowShopDialogue(int line)
    {
        string hero=game.State.heroIsBoy?"HRDINA":"ARABELLA";
        string[] speakers={"OBCHODNICE",hero,"OBCHODNICE",hero};
        string[] lines={
            "Vítej v mém obchodě. Cesta do Havranova byla jistě dlouhá. Co pro tebe mohu udělat?",
            "Dobrý den. Ve vesnici se mluví o nájezdnících. Potřebuji zásoby a pořádnou výbavu, než vyrazím dál.",
            "Mám léčivé lektvary a mohu ti vylepšit meč i zbroj. A pokud chceš pomoci, čeká tu zásilka pro strážného Petra.",
            "Děkuji. Ukažte mi prosím zboží. Chci si poslechnout, s čím vám mohu pomoci."};
        line=Mathf.Clamp(line,0,lines.Length-1);
        Open("shopDialogue",speakers[line],lines[line]);
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
        Action("Tajemství sklepa", ShowCellarMerchant);
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
        if(state.SideQuestStage(id)==1) Action("Sledovat na mapce",()=>game.TrackQuest(id));
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
        if(game.State.cellarQuest != 7) { ShowCellarGuide(); return; }
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
        if(!game.IsTestLocation){ShowLocationMap();return;}
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
            image.color=water ? new Color(.17f,.65f,.72f) : road ? new Color(.92f,.79f,.48f) : roof ? new Color(.73f,.39f,.20f) : new Color(.39f+(x+y)%3*.035f,.66f,.22f);
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
        string who=companion==0?"Sám / sama":companion==1?"Petr • občasná rada, bez doprovodu":"Mira • léčí během cesty";
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
        ResetGuideDialogue();
        ResetConversationLayout();
        SetGameplayHudVisible(false);
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
        if(RpgDialogueLayout.IsConversation(kind))
        {
            ApplyConversationLayout(kind,title);
            if(kind=="cellarGuide"||kind=="guideInstruction"||kind=="guard")BuildGuideDialogue();
        }
    }

    private void Action(string title, Action callback)
    {
        int index=actions.childCount;
        float width=conversationActive?RpgDialogueLayout.ButtonWidth:696;
        float height=conversationActive?RpgDialogueLayout.ButtonHeight:currentModal=="journal"?32:42;
        float spacing=currentModal=="journal"?38:50;
        var position=conversationActive?RpgDialogueLayout.ButtonPosition(index):new Vector2(0,-index*spacing);
        var rect=Rect(actions,title,position,new Vector2(width,height));
        var image=rect.gameObject.AddComponent<Image>();RpgVisualTheme.Panel(image);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.highlightedColor=new Color(1,.88f,.6f);colors.pressedColor=new Color(.6f,.7f,.6f);button.colors=colors;
        button.onClick.AddListener(()=>callback());
        var text=Label(rect,title,new Vector2(12,0),new Vector2(width-24,height),conversationActive?19:21,Gold);
        text.alignment=TextAnchor.MiddleCenter;text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=conversationActive?19:21;
    }
    private void SetGameplayHudVisible(bool visible)
    {
        if(gameplayHud==null)return;
        gameplayHud.alpha=visible?1:0;gameplayHud.interactable=visible;gameplayHud.blocksRaycasts=visible;
    }

    private Image Bar(Transform parent, Vector2 position, Color color, float height=12)
    {
        var track = Panel(parent, "Bar", position, new Vector2(324, height), new Vector2(0, 1));
        track.GetComponent<Image>().sprite=null;track.GetComponent<Image>().color = new Color(.12f,.085f,.06f);
        var fillRect = Rect(track, "Fill", new Vector2(1,-1), new Vector2(322, height-2));
        var image = fillRect.gameObject.AddComponent<Image>(); image.color = color; image.sprite = RpgGame.MarkerSprite();
        // A white one-pixel texture makes a rectangular filled bar.
        image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f);
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.raycastTarget = false;
        return image;
    }

    private RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor)
    {
        var rect = Rect(parent, name, Vector2.zero, size); Anchor(rect, anchor, position);
        var image = rect.gameObject.AddComponent<Image>(); RpgVisualTheme.Panel(image);
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
