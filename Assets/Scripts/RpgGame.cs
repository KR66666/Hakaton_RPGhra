using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed partial class RpgGame : MonoBehaviour
{
    public static RpgGame Instance { get; private set; }
    public RpgState State { get; private set; }
    public bool IsTestLocation { get; private set; }
    public static bool AllowsHostileCombat(int area, bool testLocation, int quest) => quest == 1 && (area > 0 || testLocation);
    public bool HostileCombatAllowed => !State.storyComplete && ((!InTown && State.cycle>=2) || (InTown || IsTestLocation) && AllowsHostileCombat(State.currentArea, IsTestLocation, State.questStage));
    public RpgInterface Interface { get; private set; }
    public bool IsEnteringRoom { get; private set; }
    public bool BlockMovement => InBattle || RpgSettings.CapturesInput || IsEnteringRoom || !Application.isFocused || Interface != null && Interface.IsModal || State == null || State.health <= 0;
    public float SpeedMultiplier => BlockMovement ? 0 : (Sprinting ? 1.5f : 1);
    public Vector2 Facing { get; set; } = Vector2.down;
    public string NearbyPrompt { get; private set; }
    public float AttackCooldown => Mathf.Clamp01((nextAttack - Time.time) / 0.42f);
    public List<RpgEnemy> Enemies { get; } = new List<RpgEnemy>();
    public Transform Player => transform;
    public Sprite HeroSprite { get; private set; }
    public Sprite MapSprite => map!=null?map.sprite:null;
    public Bounds MapBounds => map!=null?map.bounds:new Bounds(Vector3.zero,new Vector3(40,26.666f,1));
    private readonly List<WorldItem> worldItems = new List<WorldItem>();
    private readonly List<GameObject> worldObjects = new List<GameObject>();
    private SpriteRenderer map;
    private SpriteRenderer playerRenderer;
    private HeroWalkAnimation actorAppearance;
    private Vector3 roomReturnPosition, entryScale;
    private bool hiddenInRoom;
    private GameObject companionObject;
    private float nextCompanionHeal;
    private float nextAttack, dodgeUntil, nextDamage, nextSave, nextRegen;
    private Color playerColor;
    private bool startingNewStory;
    private string SavePath => Path.Combine(Application.persistentDataPath, "village-rpg-save.json");
    private bool Sprinting => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && State.stamina > 1;

    private sealed class WorldItem
    {
        public string id, title, kind;
        public GameObject obj;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene") return;
        foreach (var root in scene.GetRootGameObjects())
            if (root.CompareTag("Player") && root.GetComponent<RpgGame>() == null)
                root.AddComponent<RpgGame>();
    }

    private void Awake()
    {
        Instance = this;
        playerRenderer = GetComponent<SpriteRenderer>();
        actorAppearance = new HeroWalkAnimation(playerRenderer.sprite);
        HeroSprite = actorAppearance.Frame(0, 0);
        playerColor = playerRenderer.color;
        var background = GameObject.Find("Background");
        if (background != null) map = background.GetComponent<SpriteRenderer>();
        var fantasyMap=Resources.Load<Sprite>("MedievalTownHD") ?? Resources.Load<Sprite>("MedievalTownDetailed") ?? Resources.Load<Sprite>("MedievalTownExpanded") ?? Resources.Load<Sprite>("SpaciousVillage") ?? Resources.Load<Sprite>("FantasyVillage");
        if(map!=null && fantasyMap!=null)
        {
            var expanded=RpgWorldGeometry.ExpandedBounds(map.bounds);
            map.sprite=fantasyMap;map.transform.position+=expanded.center-map.bounds.center;
            map.sharedMaterial=RpgVisualTheme.FantasyAtmosphere();
        }
        CaptureTownMap();
        IsTestLocation=MainMenu.LaunchTest;MainMenu.LaunchTest=false;
        var requested=MainMenu.ConsumePendingGame(out startingNewStory);
        State = IsTestLocation ? CreateTestState(MainMenu.SelectedHeroIsBoy ?? false)
            : requested ?? ReadSave() ?? RpgState.CreateNew(MainMenu.SelectedHeroIsBoy ?? false);
        MainMenu.SelectedHeroIsBoy=null;
        if(IsTestLocation)startingNewStory=false;
        State.Normalize();
        ApplyLocationMap();
        if(State.travelCompanion==1)State.travelCompanion=0;
        HeroSprite=actorAppearance.Frame(0,0,State.heroIsBoy);
        if (State.health <= 0) State.health = State.MaxHealth;
        PlacePlayer();
        nextSave = Time.unscaledTime + 30;
    }

    private void Start()
    {
        var oldHud = FindAnyObjectByType<PlayerHud>();
        if (oldHud != null) oldHud.gameObject.SetActive(false);
        Interface = gameObject.AddComponent<RpgInterface>();
        Interface.Initialize(this);
        BuildWorld();
        if(!IsTestLocation){State.RecordArchivePlace(RpgArchive.PlaceKey(State.place));State.RecordOwnedArchiveItems();}
        if(startingNewStory){Save(false,true);startingNewStory=false;}
        Notify(IsTestLocation?"TESTOVACÍ LOKACE • E vyzkoušet • T nástroje • hlavní příběh se nepřepisuje":InTown?"Vítej v Havranově. Promluv si s průvodcem [E].":LocationName+" • "+RpgLocations.Description(State.place), 7);
        if(!IsTestLocation && InTown && !State.NpcDefeated("guard"))Interface.ShowGuideWelcome();
    }

    private void Update()
    {
        if (State == null || Interface == null || InBattle || Interface.StoryAnimating || IsEnteringRoom || RpgSettings.CapturesInput) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && Application.isFocused)
        {
            if(IsTestLocation && keyboard.tKey.wasPressedThisFrame) Interface.ShowTestTools();
            if (keyboard.f2Key.wasPressedThisFrame) Interface.ToggleHudExtras();
            if (keyboard.escapeKey.wasPressedThisFrame) Interface.TogglePause();
            if (keyboard.cKey.wasPressedThisFrame) Interface.ShowCharacterUpgrades();
            if (keyboard.mKey.wasPressedThisFrame) Interface.ShowWorldMap();
            if (keyboard.iKey.wasPressedThisFrame) Interface.ToggleInventory();
            if (keyboard.qKey.wasPressedThisFrame) Interface.ToggleJournal();
            if (keyboard.f5Key.wasPressedThisFrame) Save();
            if (keyboard.f9Key.wasPressedThisFrame) LoadGame();
            if (!BlockMovement)
            {
                if (keyboard.jKey.wasPressedThisFrame) Attack();
                if (keyboard.hKey.wasPressedThisFrame) DrinkPotion();
                if (keyboard.eKey.wasPressedThisFrame) Interact();
            }
        }

        if (!BlockMovement)
        {
            bool moving = keyboard != null && (keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed || keyboard.dKey.isPressed || keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed);
            if (Sprinting && moving) State.stamina = Mathf.Max(0, State.stamina - Time.deltaTime * 23);
            else if (Time.time >= nextRegen) State.stamina = Mathf.Min(100, State.stamina + Time.deltaTime * 21);
        }
        if (playerRenderer != null)
            playerRenderer.color = Time.time < nextDamage && State.health > 0 ? Color.Lerp(playerColor, Color.red, 0.65f) : playerColor;
        if(!IsTestLocation && InTown && !BlockMovement && !Interface.InRoom && State.NeedsGuideInstruction)
        {
            State.guideSeenStage=State.cellarQuest;
            Interface.ShowGuideInstruction();
            Save(false);
        }
        UpdateCompanion();
        UpdatePrompt();
        if (Time.unscaledTime >= nextSave && State.health > 0)
        {
            Save(false);
            nextSave = Time.unscaledTime + 30;
        }
    }

    private void Attack() => TryStartNearbyBattle();

    public void Damage(int amount)
    {
        if (IsTestLocation || State.currentArea == 0) return;
        if (BlockMovement || Time.time < nextDamage || Time.time < dodgeUntil) return;
        int damage = Mathf.Max(1, amount - State.Defense);
        State.health = Mathf.Max(0, State.health - damage);
        nextDamage = Time.time + 0.55f;
        FloatingText(transform.position + Vector3.up * 0.7f, "−" + damage, new Color(1, 0.35f, 0.3f));
        if (State.health <= 0) Interface.ShowDeath();
    }

    public void EnemyDefeated(RpgEnemy enemy)
    {
        if (State.defeated.Contains(enemy.Id)) return;
        State.defeated.Add(enemy.Id);
        State.banditsDefeated++;
        State.gold += 12;
        State.RecordArchiveEnemy(enemy.IsFlying);
        Notify(RpgEnemy.KindName(enemy.IsFlying)+" poražen • +12 zlata");
        BuildCompanion();
        Save(false);
    }

    public void DrinkPotion()
    {
        Notify(State.DrinkPotion() ? "Léčivý lektvar: +15 zdraví." : State.potions == 0 ? "Nemáš žádný lektvar. Navštiv obchodníka." : "Zdraví už je plné.");
    }

    private void BuildWorld()
    {
        foreach (var obj in worldObjects) if (obj != null) { obj.SetActive(false); Destroy(obj); }
        worldObjects.Clear(); worldItems.Clear(); Enemies.Clear();
        if(IsTestLocation){BuildTestLocation();return;}
        if(BuildExplorationLocation())return;
        AddLocationExits();
        AddPerson("mayor", "Rychtář Radovan", "mayor", new Vector2(-2.1f, 0.8f), new Color(0.85f, 0.72f, 0.35f));
        AddShopEntrance();
        AddVillageLandmark("KAMENNÝ MOST",new Vector2(1360,520));
        AddExpandedDistrict("HRADNÍ BRÁNA",new Vector2(155,190),"Strážný Bohumil");
        AddExpandedDistrict("ŘEMESLNICKÁ ULIČKA",new Vector2(140,430),"Kovář Mikuláš");
        AddExpandedDistrict("TRŽIŠTĚ",new Vector2(790,850),"Trhovkyně Alena");
        AddExpandedDistrict("VODNÍ MLÝN",new Vector2(1380,185),"Mlynář Matěj");
        AddExpandedDistrict("PŘÍSTAVIŠTĚ",new Vector2(1340,570),"Rybář Oldřich");
        AddExpandedDistrict("SEDLÁCKÁ ČTVRŤ",new Vector2(250,640),"Selka Dorota");
        AddPerson("elder", "Děda Matěj", "elder", new Vector2(-4, 1.3f), Color.white);
        AddPerson("healer", "Bylinkářka Mira", "healer", new Vector2(-1.6f, -2.2f), new Color(0.55f, 1, 0.7f));
        AddPerson("guard", "Petr Kříž • průvodce", "guard", new Vector2(13.5f, 3.3f), new Color(0.6f, 0.7f, 1));
        AddQuestContainers();



        Vector2[] spawns = { new Vector2(-4.7f, 3.6f), new Vector2(4.8f, 3.8f), new Vector2(-4.2f, -3.7f), new Vector2(4.8f, -3.8f), new Vector2(0.4f, 5.7f), new Vector2(5.8f, 0.4f) };
        for (int i = 0; i < spawns.Length; i++) SpawnEnemy("bandit" + i, spawns[i], false);
        BuildCompanion();
    }

    public static RpgState CreateTestState(bool boy)
    {
        var state=new RpgState {heroIsBoy=boy,upgradePoints=4,gold=9999,potions=30,cycle=2,hasIronSword=true,swordTrained=true,bowTrained=true,hasBow=true,suppliesQuest=1,deliveryQuest=1,x=-3,y=-.5f};
        state.health=state.MaxHealth;return state;
    }
    public void ResetTestLocation()
    {
        if(!IsTestLocation)return;
        RestoreOutdoorPlayer();State=CreateTestState(State.heroIsBoy);PlacePlayer();BuildWorld();Interface.Close();Notify("Testovací lokace obnovena.");
    }
    public void ToggleTestCombat()
    {
        if(!IsTestLocation)return;
        State.questStage=State.questStage==1?0:1;Interface.Close();Notify(State.questStage==1?"Tahové souboje zapnuté":"Souboj vypnutý");
    }
    private void BuildTestLocation()
    {
        AddPerson("mayor","Rychtář Radovan","mayor",new Vector2(-2,6.8f),Color.white);
        AddPerson("merchant","Obchodnice Eliška","merchant",new Vector2(0,6.8f),Color.white);
        AddPerson("healer","Bylinkářka Mira","healer",new Vector2(2,6.8f),Color.white);
        AddPerson("guard","Petr Kříž • průvodce","guard",new Vector2(4,6.8f),Color.white);
        AddPerson("elder","Děda Matěj","elder",new Vector2(5.5f,6.8f),Color.white);
        AddPickup("testTools","Testovací nástroje [T]","testgear",new Vector2(-3,3.3f),Color.cyan);
        AddPickup("testPotion","Léčivý lektvar","testpotion",new Vector2(-2,4.7f),new Color(.8f,.2f,.4f));
        AddPickup("chestTest","Truhla s odměnou","chest",new Vector2(4,4.7f),Color.yellow);
        AddPickup("supply1","Zásoby • bedna 1","supply",new Vector2(-2,3.3f),new Color(1,.6f,.2f));
        AddPickup("supply2","Zásoby • bedna 2","supply",new Vector2(0,3.3f),new Color(1,.6f,.2f));
        for(int rank=0;rank<=5;rank++)
        {
            AddPickup("testWeapon"+rank,"Meč +"+rank,"testweapon",new Vector2(-3+rank*1.4f,1.9f),new Color(.75f,.85f,1));
            AddPickup("testArmor"+rank,"Zbroj +"+rank,"testarmor",new Vector2(-3+rank*1.4f,.5f),new Color(.65f,.7f,.8f));
        }
        for(int i=0;i<5;i++)SpawnEnemy((i%2==0?"wolf":"flying")+i,new Vector2(-3+i*2.2f,5.9f),false);
    }

    private void SpawnEnemy(string id, Vector2 position, bool boss)
    {
        if(InTown && State.currentArea == 0 && !IsTestLocation)
        {
            if(boss) return;
            string[] residents={"Pekař Ondřej", "Tesař Jakub", "Selka Anna", "Kupec Václav", "Mlynář Jan", "Švadlena Marie"};
            int index=id.Length>0 && char.IsDigit(id[id.Length-1]) ? (id[id.Length-1]-'0')%residents.Length : 0;
            AddPerson(id, residents[index], "villager", position, Color.white);
            return;
        }
        if (State.defeated.Contains(id)) return;
        if (State.currentArea > 0) position = RpgCampaign.Starts[State.currentArea] + (position - new Vector2(0, 2)) * 0.55f;
        position = SnapToRoad(position);
        var obj = new GameObject(RpgEnemy.KindName(id.StartsWith("flying")));
        obj.transform.position = position;
        var enemy = obj.AddComponent<RpgEnemy>();
        enemy.Initialize(this, id, position, false);
        Enemies.Add(enemy); worldObjects.Add(obj);
    }

    private void AddPerson(string id, string title, string kind, Vector2 position, Color tint)
    {
        if(State.NpcDefeated(id))return;
        var obj = new GameObject(title);
        obj.transform.position = SnapToRoad(position);
        obj.transform.localScale = Vector3.one * 0.9f;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = (kind=="guard"?RpgDirectionalHero.PetrFrame(0,0):kind=="elder"?RpgDirectionalHero.MatejFrame(0,0):null) ?? MedievalNpcAppearance.Get(kind=="elder"?"healer":kind,kind=="elder"?3:0);
        // Keep Unity's native sprite material: the palette/UV filter is for scenery,
        // and can change fine actor details as render batches and camera position change.
        renderer.color = Color.white; renderer.sortingOrder = 2;
        var npcLabel=AddLabel(obj.transform, title, new Vector3(0, 0.9f, 0), new Color(1, 0.9f, 0.58f));
        if(IsTestLocation)npcLabel.characterSize=.05f;
        obj.AddComponent<RpgNpcNameplate>().Initialize(this,npcLabel,title,id);
        worldItems.Add(new WorldItem { id = id, title = title, kind = kind, obj = obj });
        worldObjects.Add(obj);
        if(!IsTestLocation) obj.AddComponent<VillageNpcRoutine>().Initialize(this,kind,title);
    }
    private Vector3 ShopApproach => RpgWorldGeometry.PixelPosition(RpgWorldGeometry.ShopApproachPixel,map.bounds);
    private void AddExpandedDistrict(string title,Vector2 point,string resident)
    {
        var position=RpgWorldGeometry.ExpandedPixelPosition(point,map.bounds);
        var marker=new GameObject(title);marker.transform.position=position;
        var text=AddLabel(marker.transform,title,new Vector3(0,.7f,0),new Color(1,.9f,.65f));text.characterSize=.04375f;
        worldObjects.Add(marker);
        AddPerson("district"+resident,resident,"district",position,Color.white);
    }
    private void AddVillageLandmark(string title,Vector2 point)
    {
        var landmark=new GameObject(title);landmark.transform.position=RpgWorldGeometry.PixelPosition(point,map.bounds);
        var text=AddLabel(landmark.transform,title,new Vector3(0,.35f,0),new Color(1,.9f,.65f));text.characterSize=.04375f;
        worldObjects.Add(landmark);
    }
    private void AddShopEntrance() => AddEntranceMarker("shop","Obchod","shop",ShopApproach);

    private void AddQuestContainers()
    {
        if(!State.QuestContainersVisible)return;
        AddPickup("chest1", "Opuštěná truhla", "chest", new Vector2(4.6f, 3), new Color(1, 0.75f, 0.25f));
        AddPickup("chest2", "Kupecká truhla", "chest", new Vector2(-4.5f, -2.8f), new Color(1, 0.75f, 0.25f));
        AddPickup("supply1", "Ztracené zásoby • sever náměstí", "supply", new Vector2(2.8f, 5.4f), new Color(1, 0.6f, 0.2f));
        AddPickup("supply2", "Ztracené zásoby • východní cesta", "supply", new Vector2(12.3f, 3.5f), new Color(1, 0.6f, 0.2f));
    }
    private void RefreshQuestContainers()
    {
        if(IsTestLocation || !InTown)return;
        foreach(var item in worldItems.FindAll(i=>i.kind=="chest" || i.kind=="supply"))
        {
            if(item.obj==null)continue;
            worldObjects.Remove(item.obj);item.obj.SetActive(false);Destroy(item.obj);
        }
        worldItems.RemoveAll(i=>i.kind=="chest" || i.kind=="supply");
        AddQuestContainers();
    }

    private void AddPickup(string id, string title, string kind, Vector2 position, Color tint)
    {
        if (State.collected.Contains(id)) return;
        if(!IsTestLocation && (kind=="chest" || kind=="supply") && !State.QuestContainersVisible)return;
        var obj = new GameObject(title);
        obj.transform.position = SnapToRoad(position);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = RpgVisualTheme.Icon(kind=="extraCandle"?"candle":kind); renderer.color = Color.white; renderer.sortingOrder = 3;
        obj.transform.localScale = new Vector3(0.35f, 0.35f, 1);
        if(IsTestLocation){var label=AddLabel(obj.transform,title,new Vector3(0,1.2f,0),Color.white);label.transform.localScale=new Vector3(1/.35f,1/.35f,1);}
        worldItems.Add(new WorldItem { id = id, title = title, kind = kind, obj = obj });
        worldObjects.Add(obj);
    }

    private WorldItem NearestItem()
    {
        WorldItem result = null; float distance = 2.5f;
        foreach (var item in worldItems)
        {
            if (item.obj == null || !item.obj.activeSelf) continue;
            float current = Vector2.Distance(transform.position, item.obj.transform.position);
            if (RpgEntranceRules.IsEntrance(item.kind) && current > RpgEntranceRules.Reach) continue;
            if(item.kind=="secretBook"&&current>RpgSecretBookGlow.BookReach)continue;
            if (current < distance) { result = item; distance = current; }
        }
        return result;
    }

    public Vector3 RoomDoor(string kind) => kind=="merchant" ? RpgWorldGeometry.PixelPosition(RpgWorldGeometry.ShopDoorPixel,map.bounds) : transform.position;
    public bool AtRoomDoor(string kind) => InTown && kind=="merchant" && RpgEntranceRules.InReach(transform.position,SnapToRoad(ShopApproach));
    private void UpdatePrompt()
    {
        var nearest=NearestItem();
        bool unknown=nearest!=null && !State.HasMetNpc(nearest.id) &&
            (nearest.kind=="mayor" || nearest.kind=="healer" || nearest.kind=="guard" || nearest.kind=="elder" || nearest.kind=="villager" || nearest.kind=="district" || nearest.kind=="merchant");
        NearbyPrompt=!BlockMovement && nearest!=null ? "[E] "+(unknown?"Promluvit s neznámým obyvatelem":nearest.title) : "";
        if(!BlockMovement && nearest!=null && RpgEntranceRules.IsEntrance(nearest.kind))
            NearbyPrompt=RpgEntranceRules.Prompt(nearest.title);
    }
    public void BeginRoomEntry(string kind)
    {
        if(kind!="merchant" || IsEnteringRoom || hiddenInRoom) return;
        if(!AtRoomDoor(kind)) { Notify("Přijdi ještě blíž ke dveřím."); return; }
        Interface.Close();
        BeginMiniTransition(()=>
        {
            roomReturnPosition=SnapToRoad(ShopApproach);entryScale=transform.localScale;
            playerRenderer.enabled=false;hiddenInRoom=true;
            Interface.EnterRoom(kind);
        });
    }
    public void RestoreOutdoorPlayer()
    {
        if(!hiddenInRoom)return;
        hiddenInRoom=false;playerRenderer.enabled=true;playerRenderer.color=playerColor;
        transform.position=roomReturnPosition;transform.localScale=entryScale;
    }
    private void Interact()
    {
        var item=NearestItem();
        if(item==null) { Notify("Přijdi blíž k postavě nebo předmětu.");return; }
        if(item.kind=="extraCandle"){CollectCycleCandle(false);return;}
        if(item.kind=="ritual") { TryRitual();return; }
        if(item.kind=="locationExit") { OfferLocationEntry((RpgPlace)int.Parse(item.id));return; }
        if(item.kind=="shop") { Interface.ShowRoomEntrance("merchant","Obchod");return; }
        if(item.kind=="secretBook") {PullLibraryBook(item.obj);return;}
        if(item.kind=="archiveCandle")
        {
            CollectCycleCandle(true);
            return;
        }
        DialogueNpcId=item.id;
        if(item.kind=="merchant" && IsTestLocation) {Interface.EnterRoom("merchant");return;}
        if(item.kind=="testgear") {Interface.ShowTestTools();return;}
        if(item.kind=="testpotion") {State.potions++;item.obj.SetActive(false);Notify("Testovací lektvar přidaný do batohu.");return;}
        if(item.kind=="testweapon") {State.hasIronSword=true;State.weaponLevel=Mathf.Clamp(int.Parse(item.id.Substring(10)),0,5);Notify("Nasazen meč +"+State.weaponLevel);return;}
        if(item.kind=="testarmor") {State.armorLevel=Mathf.Clamp(int.Parse(item.id.Substring(9)),0,5);Notify("Nasazena zbroj +"+State.armorLevel);return;}
        if(item.kind=="mayor") Interface.ShowMayor();
        else if(item.kind=="healer") Interface.ShowHealer();
        else if(item.kind=="guard") Interface.ShowGuard();
        else if(item.kind=="elder") Interface.ShowCellarElder();
        else if(item.kind=="villager" || item.kind=="district") Interface.ShowResident(item.title);
        else
        {
            if (item.kind == "supply" && State.suppliesQuest == 0)
            {
                Notify("Tyto zásoby patří vesnici. Zeptej se rychtáře na ztracené zásoby.");
                return;
            }
            State.collected.Add(item.id);
            item.obj.SetActive(false);
            if (item.kind == "chest") { State.gold += 30; State.potions++; Notify("Truhla: +30 zlata a léčivý lektvar."); }
            else if (item.kind == "supply") Notify($"Zásoby nalezeny: {State.SuppliesFound}/2. Odevzdej je rychtáři.");
            Save(false);
        }
    }

    public void AcceptQuest()
    {
        State.AcceptQuest(); State.trackedQuest=-1; Interface.Close();
        Notify("Úkol přijat: poraz 5 vlků nebo netopýrů.", 6);
        Save(false);
    }

    public void AcceptSideQuest(int id)
    {
        if (!State.AcceptSideQuest(id)) return;
        State.trackedQuest=id;
        if(id==1)RefreshQuestContainers();
        Interface.Close(); Notify("Nový úkol přijat. Podrobnosti najdeš v deníku [Q].", 5); Save(false);
    }

    public void CompleteSideQuest(int id)
    {
        int points=State.UpgradePoints;
        if (!State.FinishSideQuest(id)) { Notify("Ještě nemáš vše potřebné k odevzdání úkolu."); return; }
        if(id==1)RefreshQuestContainers();
        Interface.Close(); Notify(State.UpgradePoints>points?"Úkol splněn! Získáváš 1 bod pro odemčení dovednosti a odměnu.":"Úkol splněn! Odměna získána; bodový limit cyklu už je vyčerpán.", 5); Save(false);
    }

    public void DeliverParcel()
    {
        if (State.deliveryQuest != 1 || State.parcelDelivered) return;
        State.parcelDelivered = true;
        Interface.Close(); Notify("Balíček předán Petrovi. Vrať se k Elišce pro odměnu.", 5); Save(false);
    }

    public void ClaimReward()
    {
        if (!State.FinishQuest()) return;
        if (!State.completedAreas.Contains(State.currentArea)) State.completedAreas.Add(State.currentArea);
        Save(false); Interface.ShowVictory();
    }

    public void TravelToArea(int area, int companion, bool cart, bool replay)
    {
        if(!InTown){Notify("Na výpravu můžeš vyrazit z Havranova.");return;}
        string problem = State.TravelProblem(area, companion, cart);
        if (problem != null) { Notify(problem); return; }
        if (!State.EnterArea(area, companion, cart, replay)) return;
        var spawn = SnapToRoad(RpgCampaign.Starts[area]);
        State.x = spawn.x; State.y = spawn.y;
        PlacePlayer(); BuildWorld(); Interface.Close(); Save(false);
        Notify(RpgCampaign.Names[area] + (replay ? " • opakování" : " • cesta dokončena"), 5);
    }

    private void BuildCompanion()
    {
        if(companionObject!=null){worldObjects.Remove(companionObject);Destroy(companionObject);}
        companionObject = null;
        // Petr is a portrait adviser, never a following world actor (including older saves).
        if (State.travelCompanion != 2 || State.NpcDefeated("healer")) return;
        companionObject = new GameObject(State.travelCompanion != 2 ? "Petr Kříž • průvodce" : "Doprovod Mira");
        companionObject.transform.position = SnapToRoad(transform.position + Vector3.left);
        var renderer = companionObject.AddComponent<SpriteRenderer>();
        renderer.sprite = MedievalNpcAppearance.Get(State.travelCompanion != 2 ? "guard" : "healer"); renderer.sortingOrder = 3;
        renderer.color = Color.white;
        companionObject.transform.localScale = Vector3.one * .75f;
        AddLabel(companionObject.transform, companionObject.name, new Vector3(0,.9f,0), Color.cyan);
        worldObjects.Add(companionObject);
    }
    private void UpdateCompanion()
    {
        if (companionObject == null || BlockMovement) return;
        Vector3 current = companionObject.transform.position;
        if (Vector2.Distance(current, transform.position) > 1.2f)
            companionObject.transform.position = MoveEnemy(current, Vector3.MoveTowards(current, transform.position, Time.deltaTime * 3.8f));
        if (State.travelCompanion == 2 && Time.time >= nextCompanionHeal && State.health < State.MaxHealth)
        { State.health = Mathf.Min(State.MaxHealth, State.health + 5); nextCompanionHeal = Time.time + 8; }

    }

    public void TrackQuest(int id)
    {
        if(id< -1 || id>2)return;
        trackedLocation=null;trackedShop=false;
        State.trackedQuest=id;Save(false);Interface.Close();Notify("Cíl označený na malé mapě.");
    }
    public bool TryTrackedGoal(out Vector3 position,out string description)
    {
        position=transform.position;description="";
        if(TryLocationGoal(out position,out description))return true;
        if(!InTown)return false;
        if(State.cellarQuest==6 && State.NpcDefeated("guard"))return false;
        if (State.cellarQuest > 0 && State.cellarQuest < 7 && State.currentArea == 0)
        {
            string cellarTargetKind = State.cellarQuest == 1 ? "elder" : State.cellarQuest == 6 ? "guard" : "shop";
            var storyTarget = worldItems.Find(i => i.kind == cellarTargetKind && i.obj != null);
            if(storyTarget != null) { position=storyTarget.obj.transform.position; description=State.CellarObjective; return true; }
        }
        if(State.currentArea==0 && State.cellarQuest==0 && State.trackedQuest<0)
        {
            var guide=worldItems.Find(i=>i.kind=="guard" && i.obj!=null);
            if(guide!=null){position=guide.obj.transform.position;description="Promluv s průvodcem Petrem Křížem";return true;}
        }
        int quest=State.trackedQuest;
        if(quest>=0 && State.SideQuestStage(quest)!=1)quest=-1;
        string kind=null;
        if(quest==1) { kind=State.SuppliesFound>=2?"mayor":"supply";description=State.SuppliesFound>=2?"Odevzdej zásoby Radovanovi":"Najdi ztracené zásoby"; }
        else if(quest==2) { kind=State.parcelDelivered?"shop":"guard";description=State.parcelDelivered?"Vrať se k obchodnici":"Doruč zásilku Petrovi"; }
        else if(State.questStage==0 || State.CanFinishQuest) {kind="mayor";description=State.CanFinishQuest?"Vyzvedni odměnu":"Promluv s Radovanem";}
        else if(State.questStage==1 && !State.CanFinishQuest)
        {
            float closest=float.MaxValue;bool found=false;
            foreach(var enemy in Enemies)
            {
                if(enemy==null||enemy.Dead)continue;
                float distance=Vector2.Distance(transform.position,enemy.transform.position);
                if(distance<closest){closest=distance;position=enemy.transform.position;description="Poraz protivníka: "+RpgEnemy.KindName(enemy.IsFlying);found=true;}
            }
            return found;
        }
        if(kind==null)return false;
        float nearest=float.MaxValue;bool target=false;
        foreach(var item in worldItems)
        {
            if(item.obj==null||!item.obj.activeSelf||item.kind!=kind)continue;
            float distance=Vector2.Distance(transform.position,item.obj.transform.position);
            if(distance<nearest){nearest=distance;position=item.obj.transform.position;target=true;}
        }
        return target;
    }

    public void UpgradeCharacter(int attribute)
    {
        if(!State.UpgradeAttribute(attribute)) { Notify("Potřebuješ bod vylepšení, nebo už je dovednost odemčená.");return; }
        Save(false);Interface.ShowCharacterUpgrades();
    }

    public void BuyPotion()
    {
        if (!State.Spend(15)) { Notify("Potřebuješ 15 zlata."); return; }
        State.potions++; Notify("Koupen léčivý lektvar."); Save(false);
    }

    public void BuyIronSword()
    {
        if(DialogueNpcId!="districtKovář Mikuláš" || !State.BuyIronSword()){Notify("Meč lze koupit u kováře od druhého cyklu za 40 zlata.");return;}
        Notify("Koupen železný meč.");Save(false);
    }

    public void Upgrade(bool weapon)
    {
        if(weapon && (!State.hasIronSword || State.cycle<2 || DialogueNpcId!="districtKovář Mikuláš")){Notify("Meč nejprve kup u kováře ve druhém cyklu.");return;}
        int rank = weapon ? State.weaponLevel : State.armorLevel;
        if (rank >= 5) { Notify("Vybavení už má nejvyšší úroveň."); return; }
        int cost = 40 + rank * 30;
        if (!State.Spend(cost)) { Notify($"Potřebuješ {cost} zlata."); return; }
        if (weapon) State.weaponLevel++; else State.armorLevel++;
        Notify(weapon ? "Meč vylepšen: +9 poškození." : "Zbroj vylepšena: −3 příchozí poškození."); Save(false);
    }

    public void Heal()
    {
        if (State.health >= State.MaxHealth) { Notify("Jsi úplně zdráv."); return; }
        if (!State.Spend(10)) { Notify("Na ošetření potřebuješ 10 zlata."); return; }
        State.health = State.MaxHealth; State.stamina = 100; Notify("Mira tě plně vyléčila."); Save(false);
    }

    public void Respawn()
    {
        State.deaths++; State.gold = Mathf.Max(0, State.gold - 10);
        State.health = State.MaxHealth; State.stamina = 100;
        State.x = 0; State.y = 0; PlacePlayer();
        nextDamage = Time.time + 3;
        foreach (var enemy in Enemies) if (enemy != null) enemy.ResetHome();
        Interface.Close(); Notify(State.NpcDefeated("healer")?"Probouzíš se po porážce. Ztratil jsi až 10 zlata.":"Mira tě zachránila. Ztratil jsi až 10 zlata."); Save(false);
    }

    private void PlacePlayer()
    {
        if (map != null)
        {
            var bounds = map.bounds;
            State.x = Mathf.Clamp(State.x, bounds.min.x + 1, bounds.max.x - 1);
            State.y = Mathf.Clamp(State.y, bounds.min.y + 1, bounds.max.y - 1);
        }
        transform.position = new Vector3(State.x, State.y, transform.position.z);
        if (map != null)
        {
            transform.position = SnapToRoad(transform.position);
            State.x = transform.position.x;
            State.y = transform.position.y;
        }
    }

    private RpgState ReadSave()
    {
        if(IsTestLocation)return null;
        return TryReadStorySave(out var saved)?saved:null;
    }
    public static bool TryReadStorySave(out RpgState saved)
    {
        var path=Path.Combine(Application.persistentDataPath,"village-rpg-save.json");
        return TryReadStorySaveFile(path,out saved)||TryReadStorySaveFile(path+".bak",out saved);
    }
    private static bool TryReadStorySaveFile(string path,out RpgState saved)
    {
        saved=null;
        try
        {
            if (!File.Exists(path)) return false;
            var value = JsonUtility.FromJson<RpgState>(File.ReadAllText(path));
            if(value==null||value.version!=1)return false;
            saved=value;return true;
        }
        catch (Exception exception) { Debug.LogWarning("Uloženou hru nelze načíst: " + exception.Message); return false; }
    }

    public void Save(bool announce = true, bool resetBackup = false)
    {
        if(InBattle){if(announce)Notify("Uložit lze po dokončení souboje.");return;}
        if(IsTestLocation){if(announce)Notify("Testovací verze se neukládá do hlavního příběhu.");return;}
        if (State == null || State.health <= 0) return;
        try
        {
            State.RecordArchivePlace(RpgArchive.PlaceKey(State.place));State.RecordOwnedArchiveItems();
            State.x = transform.position.x; State.y = transform.position.y;
            Directory.CreateDirectory(Application.persistentDataPath);
            var temporary = SavePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(State, true));
            if (File.Exists(SavePath)) File.Replace(temporary, SavePath, SavePath + ".bak");
            else File.Move(temporary, SavePath);
            if(resetBackup)File.Copy(SavePath,SavePath+".bak",true);
            if (announce) Notify("Hra uložena.");
        }
        catch (Exception exception) { Debug.LogWarning("Uložení hry selhalo: " + exception.Message); Notify("Hru se nepodařilo uložit."); }
    }

    public void ReturnToMenu()
    {
        Save(false); SceneManager.LoadScene("MainMenu");
    }

    public void NewGame()
    {
        if(IsTestLocation){ResetTestLocation();return;}
        RestoreState(RpgState.CreateNew(State.heroIsBoy,State.HeroName));Save(false,true);
        Interface.ShowGuideWelcome();
        Notify("Nový příběh začíná. Promluv si s průvodcem.");
    }

    public void LoadGame()
    {
        if(IsTestLocation){ResetTestLocation();return;}
        var saved = ReadSave();
        if (saved == null) { Notify("Zatím nemáš uloženou hru."); return; }
        RestoreState(saved);
        Notify("Uložená hra načtena.");
    }

    public void RestoreState(RpgState saved)
    {
        StopAutoBattle();finalBattle=false;battle=null;battleEnemy=null;battleNpcId=null;DialogueNpcId=null;
        State = saved; State.Normalize(); ApplyLocationMap();
        if (State.health <= 0) State.health = State.MaxHealth;
        nextAttack = dodgeUntil = nextDamage = nextRegen = 0;
        nextCompanionHeal=0;nextSave=Time.unscaledTime+30;
        Interface.CloseForRestore(); PlacePlayer(); BuildWorld();
        Facing=Vector2.down;playerRenderer.color=playerColor;playerRenderer.enabled=true;
        HeroSprite=actorAppearance.Frame(0,0,State.heroIsBoy);
        GetComponent<PlayerMovement>()?.ResetPose();
    }

    public void Notify(string text, float duration = 3.5f)
    {
        if (Interface != null) Interface.Notify(text, duration);
    }

    public Vector3 MoveEnemy(Vector3 current, Vector3 desired)
    {
        return MoveInCurrentLocation(current, desired);
    }

    public Vector3 SnapToRoad(Vector3 position)
    {
        if(!InTown && !IsTestLocation)return RpgLocations.SafePosition(State.place,position);
        return map != null ? RpgWorldGeometry.NearestWalkable(position, map.bounds) : position;
    }

    private void OnApplicationPause(bool paused) { if (paused) Save(false); }
    private void OnApplicationQuit() { Save(false); }
    private void OnDestroy()
    {
        if (Instance == this) { Save(false); Instance = null; }
        actorAppearance?.Dispose();
    }

    public static TextMesh AddLabel(Transform parent, string text, Vector3 offset, Color color)
    {
        var obj = new GameObject("Label"); obj.transform.SetParent(parent, false); obj.transform.localPosition = offset;
        var label = obj.AddComponent<TextMesh>(); label.text = text; label.fontSize = 48;
        label.characterSize = 0.06875f; label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.color = color;
        label.GetComponent<MeshRenderer>().sortingOrder = 25;
        return label;
    }

    public static void FloatingText(Vector3 position, string text, Color color)
    {
        var label = AddLabel(null, text, position, color);
        label.characterSize = 0.10f;
        label.gameObject.AddComponent<RpgFloatingText>();
    }

    private static Sprite marker;
    public static Sprite MarkerSprite()
    {
        if (marker != null) return marker;
        var texture = new Texture2D(16, 16) { filterMode = FilterMode.Point };
        var colors = new Color[256];
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            colors[y * 16 + x] = Mathf.Abs(x - 7.5f) + Mathf.Abs(y - 7.5f) < 8 ? Color.white : Color.clear;
        texture.SetPixels(colors); texture.Apply();
        marker = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        return marker;
    }
}

public sealed class RpgFloatingText : MonoBehaviour
{
    private float age;
    private void Update()
    {
        transform.position += Vector3.up * Time.deltaTime * 0.7f;
        age += Time.deltaTime;
        if (age > 0.9f) Destroy(gameObject);
    }
}
