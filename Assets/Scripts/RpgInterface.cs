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

        var tracker = Panel(canvas, "Quest tracker", new Vector2(-20, -20), new Vector2(370, 166), new Vector2(1, 1));
        Label(tracker, "DENÍK DOBRODRUHA  [Q]", new Vector2(18, -12), new Vector2(335, 26), 18, Gold);
        quest = Label(tracker, "", new Vector2(18, -44), new Vector2(335, 112), 18, new Color(0.88f, 0.9f, 0.92f));

        var controls = Panel(canvas, "Controls", new Vector2(0, 16), new Vector2(1100, 72), new Vector2(0.5f, 0));
        combat = Label(controls, "", new Vector2(18, -9), new Vector2(1064, 25), 18, Gold);
        var keys = Label(controls, "WASD / šipky  pohyb     Shift  běh     Mezerník  úhyb     E  mluvit / sebrat     I  batoh     Q  deník     Esc  pauza", new Vector2(18, -39), new Vector2(1064, 24), 16, Color.white);
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
        modal.gameObject.SetActive(false); currentModal = null;
    }

    public void TogglePause()
    {
        if (game.State.health <= 0) return;
        if (IsModal) { Close(); return; }
        Open("pause", "ODPOČINEK U OHNĚ", "Hra je pozastavena.\n\nPostup se ukládá automaticky každých 30 sekund a po důležitých událostech.\nF5: ručně uložit • F9: načíst uloženou hru.");
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
        Action("Vypít lektvar", () => { game.DrinkPotion(); ShowInventory(); });
        Action("Zavřít  [I / Esc]", Close);
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

    public void ShowMerchant()
    {
        var state = game.State;
        Open("merchant", "ELIŠKA • OBCHOD A KOVÁRNA", $"„Dobrý meč a zásoba lektvarů. Víc dobrodruh nepotřebuje.“\n\nTvé zlato: {state.gold}\nLektvary: {state.potions}   •   Meč +{state.weaponLevel}   •   Zbroj +{state.armorLevel}\n\nLektvar: +60 zdraví.\nMeč: +9 poškození za stupeň. Zbroj: −3 zranění za stupeň.");
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
        Action("Pokračovat v dobrodružství", Close);
        Action("Uložit a odejít do nabídky", game.ReturnToMenu);
    }

    private void ConfirmNewGame()
    {
        Open("new", "ZAČÍT NOVÝ PŘÍBĚH?", "Nový příběh nahradí současnou uloženou hru. Začneš znovu na náměstí s počáteční výbavou.");
        Action("Ano, začít nový příběh", game.NewGame);
        Action("Zpět", () => { Close(); TogglePause(); });
    }

    private void Open(string kind, string title, string body)
    {
        currentModal = kind;
        modalBody.fontSize = 22;
        modalTitle.text = title; modalBody.text = body;
        for (int i = actions.childCount - 1; i >= 0; i--)
        {
            var child = actions.GetChild(i); child.gameObject.SetActive(false); child.SetParent(null); Destroy(child.gameObject);
        }
        modal.gameObject.SetActive(true);
    }

    private void Action(string title, Action callback)
    {
        int index = actions.childCount;
        var rect = Rect(actions, title, new Vector2(0, -index * 50), new Vector2(696, 42));
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.16f, 0.21f, 0.23f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1, 0.88f, 0.6f); colors.pressedColor = new Color(0.6f, 0.8f, 0.7f); button.colors = colors;
        button.onClick.AddListener(() => callback());
        var text = Label(rect, title, new Vector2(12, 0), new Vector2(672, 42), 21, Gold); text.alignment = TextAnchor.MiddleCenter;
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
