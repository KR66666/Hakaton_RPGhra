using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RpgGame : MonoBehaviour
{
    public static RpgGame Instance { get; private set; }
    public RpgState State { get; private set; }
    public RpgInterface Interface { get; private set; }
    public bool IsEnteringRoom { get; private set; }
    public bool BlockMovement => RpgSettings.CapturesInput || IsEnteringRoom || !Application.isFocused || Interface != null && Interface.IsModal || State == null || State.health <= 0;
    public float SpeedMultiplier => BlockMovement ? 0 : (Time.time < dodgeUntil ? 2.5f : Sprinting ? 1.5f : 1) * (1 + State.agilityRank*.04f);
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
    private string offeredRoom;
    private Vector3 roomReturnPosition, entryScale;
    private bool hiddenInRoom;
    private GameObject companionObject;
    private float nextCompanionHeal;
    private float nextAttack, dodgeUntil, nextDamage, nextSave, nextRegen;
    private Color playerColor;
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
        State = ReadSave() ?? new RpgState();
        if (MainMenu.SelectedHeroIsBoy.HasValue)
        { State.heroIsBoy = MainMenu.SelectedHeroIsBoy.Value; MainMenu.SelectedHeroIsBoy = null; }
        State.Normalize();
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
        Notify("Vítej v Havranově. Promluv si s rychtářem [E].", 7);
    }

    private void Update()
    {
        if (State == null || Interface == null || IsEnteringRoom || RpgSettings.CapturesInput) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && Application.isFocused)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) Interface.TogglePause();
            if (keyboard.cKey.wasPressedThisFrame) Interface.ShowCharacterUpgrades();
            if (keyboard.mKey.wasPressedThisFrame) Interface.ShowWorldMap();
            if (keyboard.iKey.wasPressedThisFrame) Interface.ToggleInventory();
            if (keyboard.qKey.wasPressedThisFrame) Interface.ToggleJournal();
            if (keyboard.f5Key.wasPressedThisFrame) Save();
            if (keyboard.f9Key.wasPressedThisFrame) LoadGame();
            if (!BlockMovement)
            {
                bool mouseAttack = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                if (keyboard.jKey.wasPressedThisFrame || mouseAttack) Attack();
                if (keyboard.spaceKey.wasPressedThisFrame) Dodge();
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
        UpdateCompanion();
        UpdatePrompt();
        if (Time.unscaledTime >= nextSave && State.health > 0)
        {
            Save(false);
            nextSave = Time.unscaledTime + 30;
        }
    }

    private void Attack()
    {
        RpgSettings.PlayEffect();
        if (Time.time < nextAttack) return;
        if (State.stamina < 10) { Notify("Dochází výdrž. Na chvíli přestaň útočit."); return; }
        State.stamina -= 10;
        nextRegen = Time.time + 0.6f;
        nextAttack = Time.time + 0.42f;
        var origin = (Vector2)transform.position;
        Vector2 direction = Facing;
        // Aim toward a nearby enemy, so keyboard movement and combat work together.
        float closest = 1.8f;
        foreach (var enemy in Enemies)
        {
            if (enemy == null || enemy.Dead) continue;
            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance < closest) { closest = distance; direction = ((Vector2)enemy.transform.position - origin).normalized; }
        }
        foreach (var enemy in Enemies.ToArray())
        {
            if (enemy == null || enemy.Dead) continue;
            var delta = (Vector2)enemy.transform.position - origin;
            if (delta.magnitude <= 1.7f && (delta.magnitude < 0.35f || Vector2.Dot(direction, delta.normalized) >= -0.25f))
                enemy.Hit(State.Damage, direction);
        }
        var slash = new GameObject("Sword strike");
        slash.transform.position = transform.position + (Vector3)(direction * 0.7f);
        var renderer = slash.AddComponent<SpriteRenderer>();
        renderer.sprite = MarkerSprite();
        renderer.color = new Color(1, 0.85f, 0.45f, 0.7f);
        renderer.sortingOrder = 12;
        slash.transform.localScale = new Vector3(0.65f, 0.65f, 1);
        Destroy(slash, 0.12f);
    }

    private void Dodge()
    {
        if (State.stamina < 25 || Time.time < dodgeUntil + 0.45f) return;
        State.stamina -= 25;
        dodgeUntil = Time.time + 0.23f;
        nextRegen = Time.time + 0.6f;
    }

    public void Damage(int amount)
    {
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
        if (enemy.IsBoss) State.bossDefeated = true;
        else State.banditsDefeated++;
        State.gold += enemy.IsBoss ? 75 : 12;
        bool leveled = State.AddExperience(enemy.IsBoss ? 100 : 35);
        Notify(leveled ? $"ÚROVEŇ {State.level}! Zdraví a výdrž obnoveny. Nový bod vylepšení • C." : enemy.IsBoss ? "Velitel padl! Vrať se k rychtáři pro odměnu." : $"Nájezdník poražen • +35 XP • +12 zlata ({Mathf.Min(5, State.banditsDefeated)}/5)");
        if (State.banditsDefeated >= 5 && !State.bossDefeated) SpawnBoss();
        BuildCompanion();
        Save(false);
    }

    public void DrinkPotion()
    {
        Notify(State.DrinkPotion() ? "Léčivý lektvar: +60 zdraví." : State.potions == 0 ? "Nemáš žádný lektvar. Navštiv obchodníka." : "Zdraví už je plné.");
    }

    private void BuildWorld()
    {
        foreach (var obj in worldObjects) if (obj != null) Destroy(obj);
        worldObjects.Clear(); worldItems.Clear(); Enemies.Clear();
        AddPerson("mayor", "Rychtář Radovan", "mayor", new Vector2(-2.1f, 0.8f), new Color(0.85f, 0.72f, 0.35f));
        AddShopEntrance();
        AddPerson("healer", "Bylinkářka Mira", "healer", new Vector2(-1.6f, -2.2f), new Color(0.55f, 1, 0.7f));
        AddPerson("guard", "Strážný Petr", "guard", new Vector2(13.5f, 3.3f), new Color(0.6f, 0.7f, 1));
        AddPickup("chest1", "Opuštěná truhla", "chest", new Vector2(4.6f, 3), new Color(1, 0.75f, 0.25f));
        AddPickup("chest2", "Kupecká truhla", "chest", new Vector2(-4.5f, -2.8f), new Color(1, 0.75f, 0.25f));
        AddPickup("herb1", "Léčivé byliny", "herb", new Vector2(-3.1f, 2.7f), new Color(0.4f, 1, 0.6f));
        AddPickup("herb2", "Léčivé byliny", "herb", new Vector2(4.2f, -2.7f), new Color(0.4f, 1, 0.6f));
        AddPickup("herb3", "Léčivé byliny", "herb", new Vector2(1.4f, 4.8f), new Color(0.4f, 1, 0.6f));
        AddPickup("herb4", "Léčivé byliny", "herb", new Vector2(6.4f, 2.2f), new Color(0.4f, 1, 0.6f));
        AddPickup("garden", "Miřin záhonek", "garden", new Vector2(-2.7f, -0.3f), new Color(0.4f, 1, 0.6f));
        AddPickup("herb6", "Léčivé byliny", "herb", new Vector2(1.8f, -2.5f), new Color(0.4f, 1, 0.6f));
        AddPickup("supply1", "Ztracené zásoby • sever náměstí", "supply", new Vector2(2.8f, 5.4f), new Color(1, 0.6f, 0.2f));
        AddPickup("supply2", "Ztracené zásoby • východní cesta", "supply", new Vector2(12.3f, 3.5f), new Color(1, 0.6f, 0.2f));
        Vector2[] spawns = { new Vector2(-4.7f, 3.6f), new Vector2(4.8f, 3.8f), new Vector2(-4.2f, -3.7f), new Vector2(4.8f, -3.8f), new Vector2(0.4f, 5.7f), new Vector2(5.8f, 0.4f) };
        for (int i = 0; i < spawns.Length; i++) SpawnEnemy("bandit" + i, spawns[i], false);
        if (State.banditsDefeated >= 5 && !State.bossDefeated) SpawnBoss();
        BuildCompanion();
    }

    private void SpawnBoss()
    {
        foreach (var enemy in Enemies) if (enemy != null && enemy.IsBoss && !enemy.Dead) return;
        SpawnEnemy("commander", new Vector2(2.6f, 7.3f), true);
        Notify("Velitel Vranek čeká severně od náměstí!", 6);
    }

    private void SpawnEnemy(string id, Vector2 position, bool boss)
    {
        if (State.defeated.Contains(id)) return;
        if (State.currentArea > 0) position = RpgCampaign.Starts[State.currentArea] + (position - new Vector2(0, 2)) * 0.55f;
        position = SnapToRoad(position);
        var obj = new GameObject(boss ? "Velitel Vranek" : "Nájezdník");
        obj.transform.position = position;
        var enemy = obj.AddComponent<RpgEnemy>();
        enemy.Initialize(this, id, position, boss);
        Enemies.Add(enemy); worldObjects.Add(obj);
    }

    private void AddPerson(string id, string title, string kind, Vector2 position, Color tint)
    {
        var obj = new GameObject(title);
        obj.transform.position = SnapToRoad(position);
        obj.transform.localScale = Vector3.one * 0.9f;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = MedievalNpcAppearance.Get(kind); renderer.color = Color.white; renderer.sortingOrder = 2;
        AddLabel(obj.transform, title, new Vector3(0, 0.9f, 0), new Color(1, 0.9f, 0.58f));
        worldItems.Add(new WorldItem { id = id, title = title, kind = kind, obj = obj });
        worldObjects.Add(obj);
        obj.AddComponent<VillageNpcRoutine>().Initialize(this,kind,title);
    }
    private Vector3 ShopApproach => RpgWorldGeometry.PixelPosition(new Vector2(744,411),map.bounds);
    private void AddShopEntrance()
    {
        var entrance = new GameObject("Obchod • vstup v domě na pozadí");
        entrance.transform.position = ShopApproach;
        AddLabel(entrance.transform,"OBCHOD • Eliška",new Vector3(0,.7f,0),new Color(1,.85f,.5f));
        worldItems.Add(new WorldItem { id="shop", title="Obchod • Eliška", kind="shop", obj=entrance });
        worldObjects.Add(entrance);
    }

    private void AddPickup(string id, string title, string kind, Vector2 position, Color tint)
    {
        if (State.collected.Contains(id)) return;
        var obj = new GameObject(title);
        obj.transform.position = SnapToRoad(position);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = MarkerSprite(); renderer.color = tint; renderer.sortingOrder = 3;
        obj.transform.localScale = new Vector3(0.35f, 0.35f, 1);
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
            if (current < distance) { result = item; distance = current; }
        }
        return result;
    }

    public Vector3 RoomDoor(string kind) => kind=="merchant" ? RpgWorldGeometry.PixelPosition(new Vector2(734,384),map.bounds) : transform.position;
    public bool AtRoomDoor(string kind) => kind=="merchant" && Vector2.Distance(transform.position,ShopApproach)<=.42f;
    private void UpdatePrompt()
    {
        var nearest=NearestItem();
        NearbyPrompt=!BlockMovement && nearest!=null ? "[E] "+nearest.title : "";
        float distance=Vector2.Distance(transform.position,ShopApproach);
        if(distance>1.2f) offeredRoom=null;
        if(BlockMovement)return;
        if(distance<1.2f) NearbyPrompt=AtRoomDoor("merchant")?"[E] Vejít do obchodu":"Obchod • přijdi ke vchodu domu";
        if(AtRoomDoor("merchant") && offeredRoom!="shop")
        { offeredRoom="shop";Interface.ShowRoomEntrance("merchant","Obchodnice Eliška"); }
    }
    public void BeginRoomEntry(string kind)
    {
        if(kind!="merchant" || IsEnteringRoom || hiddenInRoom) return;
        if(!AtRoomDoor(kind)) { Notify("Přijdi ještě blíž ke dveřím."); return; }
        Interface.Close();
        StartCoroutine(EnterRoomSequence(kind));
    }
    private System.Collections.IEnumerator EnterRoomSequence(string kind)
    {
        IsEnteringRoom=true;
        roomReturnPosition=ShopApproach;entryScale=transform.localScale;
        Vector3 door=RoomDoor(kind),start=transform.position;
        var movement=GetComponent<PlayerMovement>();
        float elapsed=0;
        while(elapsed<.7f)
        {
            elapsed+=Time.deltaTime;
            var previous=transform.position;
            // Only this scripted, short doorway crossing may pass the building edge.
            transform.position=Vector3.Lerp(start,door,Mathf.Clamp01(elapsed/.7f));
            Vector2 direction=door-start;
            if(movement!=null) movement.PlayEntranceStep(direction,elapsed*1.4f);
            yield return null;
        }
        elapsed=0;
        while(elapsed<.3f)
        {
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.3f);
            transform.localScale=Vector3.Lerp(entryScale,entryScale*.3f,t);
            var color=playerColor;color.a=1-t;playerRenderer.color=color;
            yield return null;
        }
        playerRenderer.enabled=false;hiddenInRoom=true;
        transform.localScale=entryScale;playerRenderer.color=playerColor;
        IsEnteringRoom=false;
        Interface.EnterRoom(kind);
    }
    public void RestoreOutdoorPlayer()
    {
        if(!hiddenInRoom)return;
        hiddenInRoom=false;playerRenderer.enabled=true;playerRenderer.color=playerColor;
        transform.position=roomReturnPosition;transform.localScale=entryScale;
    }
    private void Interact()
    {
        if(AtRoomDoor("merchant")) { Interface.ShowRoomEntrance("merchant","Obchodnice Eliška");return; }
        var item=NearestItem();
        if(item==null) { Notify("Přijdi blíž k postavě nebo předmětu.");return; }
        if(item.kind=="shop") { Notify("Přijdi těsně ke vchodu domu.");return; }
        if(item.kind=="mayor") Interface.ShowMayor();
        else if(item.kind=="healer") Interface.ShowHealer();
        else if(item.kind=="guard") Interface.ShowGuard();
        else if (item.kind == "garden")
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < State.gardenReadyAt) { Notify($"Byliny dorostou za {State.gardenReadyAt - now} s."); return; }
            State.herbs += 2; State.gardenReadyAt = now + 45;
            Notify("Nasbírány 2 byliny. Záhonek znovu doroste za 45 sekund."); Save(false);
        }
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
            else { State.herbs += 2; Notify("Nasbíráno: 2 léčivé byliny. Mira z nich vyrobí lektvar."); }
            Save(false);
        }
    }

    public void AcceptQuest()
    {
        State.AcceptQuest(); State.trackedQuest=-1; Interface.Close();
        Notify("Úkol přijat: poraz 5 nájezdníků a jejich velitele.", 6);
        Save(false);
    }

    public void AcceptSideQuest(int id)
    {
        if (!State.AcceptSideQuest(id)) return;
        State.trackedQuest=id;
        Interface.Close(); Notify("Nový úkol přijat. Podrobnosti najdeš v deníku [Q].", 5); Save(false);
    }

    public void CompleteSideQuest(int id)
    {
        if (!State.FinishSideQuest(id)) { Notify("Ještě nemáš vše potřebné k odevzdání úkolu."); return; }
        Interface.Close(); Notify("Úkol splněn! Získáváš zlato, zkušenosti a odměnu.", 5); Save(false);
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
        companionObject = null;
        if (State.travelCompanion == 0) return;
        companionObject = new GameObject(State.travelCompanion == 1 ? "Doprovod Petr" : "Doprovod Mira");
        companionObject.transform.position = SnapToRoad(transform.position + Vector3.left);
        var renderer = companionObject.AddComponent<SpriteRenderer>();
        renderer.sprite = MedievalNpcAppearance.Get(State.travelCompanion == 1 ? "guard" : "healer"); renderer.sortingOrder = 3;
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
        if (State.travelCompanion == 1 && Time.time >= nextCompanionHeal)
        {
            foreach (var enemy in Enemies)
                if (enemy != null && !enemy.Dead && Vector2.Distance(enemy.transform.position, companionObject.transform.position) < 2)
                { enemy.Hit(8, Facing); nextCompanionHeal = Time.time + 2; break; }
        }
    }

    public void TrackQuest(int id)
    {
        if(id< -1 || id>2)return;
        State.trackedQuest=id;Save(false);Interface.Close();Notify("Cíl označený na malé mapě.");
    }
    public bool TryTrackedGoal(out Vector3 position,out string description)
    {
        position=transform.position;description="";
        int quest=State.trackedQuest;
        if(quest>=0 && State.SideQuestStage(quest)!=1)quest=-1;
        string kind=null;
        if(quest==0) { kind=State.herbs>=4?"healer":"herb";description=State.herbs>=4?"Odevzdej byliny Miře":"Nasbírej byliny"; }
        else if(quest==1) { kind=State.SuppliesFound>=2?"mayor":"supply";description=State.SuppliesFound>=2?"Odevzdej zásoby Radovanovi":"Najdi ztracené zásoby"; }
        else if(quest==2) { kind=State.parcelDelivered?"shop":"guard";description=State.parcelDelivered?"Vrať se k obchodnici":"Doruč zásilku Petrovi"; }
        else if(State.questStage==0 || State.CanFinishQuest) {kind="mayor";description=State.CanFinishQuest?"Vyzvedni odměnu":"Promluv s Radovanem";}
        else if(State.questStage==1 && !State.bossDefeated)
        {
            float closest=float.MaxValue;bool found=false;
            foreach(var enemy in Enemies)
            {
                if(enemy==null||enemy.Dead || State.banditsDefeated>=5&&!enemy.IsBoss)continue;
                float distance=Vector2.Distance(transform.position,enemy.transform.position);
                if(distance<closest){closest=distance;position=enemy.transform.position;description=enemy.IsBoss?"Poraz velitele":"Poraz nájezdníky";found=true;}
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
        if(!target && kind=="herb")
            foreach(var item in worldItems)if(item.kind=="garden"&&item.obj!=null){position=item.obj.transform.position;target=true;description="Miřin záhonek • dorůstá";break;}
        return target;
    }

    public void UpgradeCharacter(int attribute)
    {
        if(!State.UpgradeAttribute(attribute)) { Notify("Potřebuješ bod vylepšení nebo máš maximální stupeň.");return; }
        Save(false);Interface.ShowCharacterUpgrades();
    }

    public void BuyPotion()
    {
        if (!State.Spend(15)) { Notify("Potřebuješ 15 zlata."); return; }
        State.potions++; Notify("Koupen léčivý lektvar."); Save(false);
    }

    public void Upgrade(bool weapon)
    {
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

    public void CraftPotion()
    {
        if (State.herbs < 2) { Notify("Potřebuješ 2 léčivé byliny."); return; }
        State.herbs -= 2; State.potions++; Notify("Vyroben léčivý lektvar."); Save(false);
    }

    public void Respawn()
    {
        State.deaths++; State.gold = Mathf.Max(0, State.gold - 10);
        State.health = State.MaxHealth; State.stamina = 100;
        State.x = 0; State.y = 0; PlacePlayer();
        nextDamage = Time.time + 3;
        foreach (var enemy in Enemies) if (enemy != null) enemy.ResetHome();
        Interface.Close(); Notify("Mira tě zachránila. Ztratil jsi 10 zlata."); Save(false);
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
        try
        {
            if (!File.Exists(SavePath)) return null;
            var value = JsonUtility.FromJson<RpgState>(File.ReadAllText(SavePath));
            return value != null && value.version == 1 ? value : null;
        }
        catch (Exception exception) { Debug.LogWarning("Uloženou hru nelze načíst: " + exception.Message); return null; }
    }

    public void Save(bool announce = true)
    {
        if (State == null || State.health <= 0) return;
        try
        {
            State.x = transform.position.x; State.y = transform.position.y;
            Directory.CreateDirectory(Application.persistentDataPath);
            var temporary = SavePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(State, true));
            if (File.Exists(SavePath)) File.Replace(temporary, SavePath, SavePath + ".bak");
            else File.Move(temporary, SavePath);
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
        State = new RpgState { heroIsBoy = State.heroIsBoy }; PlacePlayer(); BuildWorld(); Interface.Close(); Save(false);
        Notify("Nový příběh začíná. Vyhledej rychtáře Radovana.");
    }

    public void LoadGame()
    {
        var saved = ReadSave();
        if (saved == null) { Notify("Zatím nemáš uloženou hru."); return; }
        RestoreState(saved);
        Notify("Uložená hra načtena.");
    }

    public void RestoreState(RpgState saved)
    {
        State = saved; State.Normalize();
        if (State.health <= 0) State.health = State.MaxHealth;
        nextAttack = dodgeUntil = nextDamage = nextRegen = 0;
        PlacePlayer(); BuildWorld(); Interface.Close();
    }

    public void Notify(string text, float duration = 3.5f)
    {
        if (Interface != null) Interface.Notify(text, duration);
    }

    public Vector3 MoveEnemy(Vector3 current, Vector3 desired)
    {
        return map != null ? RpgWorldGeometry.Move(current, desired, map.bounds) : desired;
    }

    public Vector3 SnapToRoad(Vector3 position)
    {
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
        label.characterSize = 0.055f; label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.color = color;
        label.GetComponent<MeshRenderer>().sortingOrder = 25;
        return label;
    }

    public static void FloatingText(Vector3 position, string text, Color color)
    {
        var label = AddLabel(null, text, position, color);
        label.characterSize = 0.08f;
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
