using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RpgValidation
{
    static RpgValidation()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool("RPG.Validation.v8", false))
            {
                Run();
                SessionState.SetBool("RPG.Validation.v8", true);
            }
        };
    }

    [MenuItem("Tools/RPG/Run Gameplay Checks")]
    public static void Run()
    {
        string report = Path.Combine(Application.dataPath, "../Temp/RpgValidation.txt");
        int checks = 0;
        try
        {
            var state = new RpgState();
            Check(state.level == 1 && state.MaxHealth == 100 && state.Damage == 18, "Initial stats", ref checks);
            Check(!state.FinishQuest(), "Cannot claim an unaccepted quest", ref checks);
            Check(state.AcceptQuest() && !state.AcceptQuest(), "Quest accepted only once", ref checks);
            Check(!state.FinishQuest(), "Quest requires enemies and boss", ref checks);
            state.banditsDefeated = 5;
            Check(!state.FinishQuest(), "Boss is required", ref checks);
            state.bossDefeated = true;
            Check(state.FinishQuest() && state.questStage == 2 && state.gold == 185 && state.potions == 6, "Quest rewards", ref checks);
            Check(!state.FinishQuest() && state.gold == 185, "Rewards cannot be duplicated", ref checks);
            state = new RpgState();
            Check(state.AddExperience(175) && state.level == 3 && state.experience == 35 && state.health == 140, "Multiple level gains", ref checks);
            Check(!state.DrinkPotion() && state.potions == 3, "Full-health potion is not consumed", ref checks);
            state.health = 20;
            Check(state.DrinkPotion() && state.health == 80 && state.potions == 2, "Potion heals and consumes once", ref checks);
            Check(state.Spend(15) && state.gold == 20 && !state.Spend(21) && !state.Spend(-5), "Gold cannot underflow", ref checks);
            state.weaponLevel = 2; state.armorLevel = 2;
            Check(state.Damage == 44 && state.Defense == 6, "Equipment scales stats", ref checks);
            state.collected.Add("chest1"); state.defeated.Add("bandit0");
            var restored = JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));
            restored.Normalize();
            Check(restored.level == state.level && restored.gold == state.gold && restored.collected.Contains("chest1") && restored.defeated.Contains("bandit0"), "Save round trip", ref checks);
            restored.gold = -20; restored.level = -1; restored.health = float.NaN; restored.x = float.PositiveInfinity;
            restored.Normalize();
            Check(restored.gold == 0 && restored.level == 1 && restored.health == 100 && restored.x == 0, "Invalid save values normalized", ref checks);
            var bounds = new Bounds(new Vector3(0, 2, 0), new Vector3(40, 26.666666f, 1));
            Check(!RpgWorldGeometry.Blocked(Vector3.zero, bounds, -0.45f), "Player spawn is walkable", ref checks);
            var house = new Vector3((350 - 768) / 38.4f, (512 - 350) / 38.4f + 2 + 0.45f, 0);
            Check(RpgWorldGeometry.Blocked(house, bounds, -0.45f), "House footprint blocks movement", ref checks);
            Check(!AtPixelBlocked(1410, 477, bounds), "Bridge is walkable", ref checks);
            Check(AtPixelBlocked(1390, 200, bounds), "River away from bridge is blocked", ref checks);
            Check(AtPixelBlocked(50, 50, bounds), "Grass outside paths is blocked", ref checks);
            Check(AtPixelBlocked(600, 550, bounds), "Market stalls are blocked", ref checks);
            var start = RpgWorldGeometry.PixelPosition(new Vector2(330, 285), bounds);
            var end = RpgWorldGeometry.PixelPosition(new Vector2(360, 450), bounds);
            Check(!RpgWorldGeometry.Blocked(start, bounds, RpgWorldGeometry.FeetOffset) && !RpgWorldGeometry.Blocked(end, bounds, RpgWorldGeometry.FeetOffset), "Opposite sides of roof are both roads", ref checks);
            var swept = RpgWorldGeometry.Move(start, end, bounds);
            Check(Vector2.Distance(swept, end) > 2 && !RpgWorldGeometry.Blocked(swept, bounds, RpgWorldGeometry.FeetOffset), "Long movement cannot tunnel through roof", ref checks);
            var recovered = RpgWorldGeometry.NearestWalkable(house, bounds);
            Check(!RpgWorldGeometry.Blocked(recovered, bounds, RpgWorldGeometry.FeetOffset), "Old save on a roof relocates to road", ref checks);
            Check(PlayerMovement.WalkingFrame(0.1f) == 1 && PlayerMovement.WalkingFrame(0.3f) == 0 && PlayerMovement.WalkingFrame(0.5f) == 2 && PlayerMovement.WalkingFrame(0.7f) == 0, "Walk cycle alternates left, planted, right, planted", ref checks);
            Check(PlayerMovement.FacingIndex(Vector2.up) == 1 && PlayerMovement.FacingIndex(Vector2.down) == 0 &&
                PlayerMovement.FacingIndex(Vector2.right) == 2 && PlayerMovement.FacingIndex(Vector2.left) == 3,
                "Four movement directions select four character views", ref checks);
            var originalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Characters/Hero.png");
            var animation = new HeroWalkAnimation(originalSprite);
            try
            {
                string[] names = { "South", "North", "East", "West" };
                for (int direction = 0; direction < 4; direction++)
                {
                    Check(animation.Frame(direction, 0).name == names[direction] + " Idle", "Dedicated standing pose " + names[direction], ref checks);
                    Check(animation.Frame(direction, 1).name == names[direction] + " Left foot" &&
                        animation.Frame(direction, 2).name == names[direction] + " Right foot" &&
                        animation.Frame(direction, 0) != animation.Frame(direction, 1) &&
                        animation.Frame(direction, 1) != animation.Frame(direction, 2), "Distinct alternating feet " + names[direction], ref checks);
                }
                Vector3 northSize = animation.Frame(1, 0).bounds.size;
                Vector3 eastSize = animation.Frame(2, 1).bounds.size;
                Check(Mathf.Abs(Mathf.Max(northSize.x, northSize.y) - Mathf.Max(eastSize.x, eastSize.y)) < 0.01f,
                    "Overhead directions keep the same ground-plane scale", ref checks);
            }
            finally { animation.Dispose(); }
            CheckNavigation(bounds, ref checks);
            state = new RpgState();
            Check(!state.FinishSideQuest(0) && !state.AcceptSideQuest(-1) && !state.AcceptSideQuest(3), "Side quests require valid acceptance", ref checks);
            Check(state.AcceptSideQuest(0) && !state.AcceptSideQuest(0), "Side quest accepted once", ref checks);
            state.herbs = 3;
            Check(!state.FinishSideQuest(0) && state.herbs == 3, "Herb quest requires four herbs", ref checks);
            state.herbs = 6;
            Check(state.FinishSideQuest(0) && state.herbs == 2 && state.gold == 60 && state.potions == 5, "Herb delivery consumes exact amount and pays reward", ref checks);
            Check(!state.FinishSideQuest(0) && state.gold == 60, "Herb reward cannot repeat", ref checks);
            Check(state.AcceptSideQuest(1) && !state.SideQuestReady(1), "Supplies require collection", ref checks);
            state.collected.Add("supply1"); state.collected.Add("supply1");
            Check(state.SuppliesFound == 1 && !state.SideQuestReady(1), "Duplicate collection does not count twice", ref checks);
            state.collected.Add("supply2");
            Check(state.FinishSideQuest(1) && state.gold == 120 && state.suppliesQuest == 2, "Supplies quest reward", ref checks);
            Check(state.AcceptSideQuest(2) && !state.FinishSideQuest(2), "Delivery needs guard confirmation", ref checks);
            state.parcelDelivered = true;
            Check(state.FinishSideQuest(2) && state.gold == 160 && state.potions == 6, "Delivery quest reward", ref checks);
            Check(!state.FinishSideQuest(2) && state.gold == 160, "Delivery reward cannot repeat", ref checks);
            restored = JsonUtility.FromJson<RpgState>(JsonUtility.ToJson(state));
            restored.Normalize();
            Check(restored.herbsQuest == 2 && restored.suppliesQuest == 2 && restored.deliveryQuest == 2 && restored.parcelDelivered, "Side quests survive save round trip", ref checks);
            restored = JsonUtility.FromJson<RpgState>("{\"version\":1,\"level\":2,\"gold\":80}"); restored.Normalize();
            Check(restored.level == 2 && restored.gold == 80 && restored.herbsQuest == 0 && restored.suppliesQuest == 0 && restored.deliveryQuest == 0, "Existing saves gain new quests without losing progress", ref checks);
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllText(report, $"PASS: {checks} gameplay checks\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log($"RPG: {checks} gameplay checks passed.");
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllText(report, "FAIL after " + checks + " checks: " + exception);
            Debug.LogError("RPG validation failed: " + exception);
        }
    }

    private static void Check(bool condition, string description, ref int checks)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++;
    }

    private static bool AtPixelBlocked(float x, float y, Bounds bounds)
    {
        return RpgWorldGeometry.Blocked(RpgWorldGeometry.PixelPosition(new Vector2(x, y), bounds), bounds, RpgWorldGeometry.FeetOffset);
    }

    private static void CheckNavigation(Bounds bounds, ref int checks)
    {
        const int columns = 192, rows = 128;
        var walkable = new bool[columns * rows];
        var visited = new bool[walkable.Length];
        for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
            walkable[y * columns + x] = !AtPixelBlocked(x * 8 + 4, y * 8 + 4, bounds);
        var queue = new System.Collections.Generic.Queue<int>();
        int origin = 75 * columns + 96;
        queue.Enqueue(origin); visited[origin] = true;
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue(), x = cell % columns, y = cell / columns;
            int[] neighbors = { x > 0 ? cell - 1 : -1, x + 1 < columns ? cell + 1 : -1,
                y > 0 ? cell - columns : -1, y + 1 < rows ? cell + columns : -1 };
            foreach (int next in neighbors)
                if (next >= 0 && walkable[next] && !visited[next]) { visited[next] = true; queue.Enqueue(next); }
        }
        Vector2[] goals = { new Vector2(0,0), new Vector2(-2.1f,0.8f), new Vector2(3.2f,-1.1f),
            new Vector2(-1.6f,-2.2f), new Vector2(13.5f,3.3f), new Vector2(12.3f,3.5f),
            new Vector2(2.8f,5.4f), new Vector2(2.6f,7.3f) };
        foreach (var goal in goals)
        {
            Vector3 position = RpgWorldGeometry.NearestWalkable(goal, bounds);
            int px = Mathf.FloorToInt((position.x - bounds.min.x) / bounds.size.x * columns);
            int py = Mathf.FloorToInt((bounds.max.y - position.y - RpgWorldGeometry.FeetOffset) / bounds.size.y * rows);
            bool reachable = false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                if (px + dx >= 0 && px + dx < columns && py + dy >= 0 && py + dy < rows)
                    reachable |= visited[(py + dy) * columns + px + dx];
            Check(reachable, "Road connection to quest destination " + goal, ref checks);
        }
    }

    [MenuItem("Tools/RPG/Run Live Gameplay Checks %#F8")]
    public static void RunLive()
    {
        var game = RpgGame.Instance;
        if (game == null) { Debug.LogError("Start SampleScene in Play Mode first."); return; }
        string snapshot = JsonUtility.ToJson(game.State);
        var originalPosition = game.transform.position;
        int checks = 0;
        string result;
        try
        {
            game.RestoreState(new RpgState());
            var movement = game.GetComponent<PlayerMovement>();
            var appearance = game.GetComponent<SpriteRenderer>();
            var opposite = movement.WalkingSprite(0, 2);
            Check(opposite != null, "Second walking step loads in the running game", ref checks);
            movement.MoveInDirection(Vector2.down, 0);
            var idle = appearance.sprite;
            movement.MoveInDirection(Vector2.down, 0.15f);
            Check(appearance.sprite == opposite, "Actual movement switches to opposite walking step", ref checks);
            movement.MoveInDirection(Vector2.zero, 0.1f);
            Check(appearance.sprite == idle, "Standing still restores idle pose", ref checks);
            movement.MoveInDirection(Vector2.up, 0.15f);
            Check(appearance.sprite != idle && appearance.sprite != opposite, "Walking north uses a rear-view sprite", ref checks);
            movement.MoveInDirection(Vector2.zero, 0);
            Check(appearance.sprite.name.StartsWith("North"), "Stopping preserves north-facing pose", ref checks);
            movement.MoveInDirection(Vector2.left, 0.15f);
            Check(appearance.sprite.name.StartsWith("West") && !appearance.flipX, "Walking left mirrors the side view", ref checks);
            movement.MoveInDirection(Vector2.right, 0.15f);
            Check(appearance.sprite.name.StartsWith("East") && !appearance.flipX, "Walking right uses an unmirrored side view", ref checks);
            game.RestoreState(new RpgState());
            game.AcceptQuest();
            Check(game.State.questStage == 1 && game.Enemies.Count == 6, "Quest and encounter creation", ref checks);
            var first = game.Enemies[0];
            first.Hit(1000, Vector2.right);
            Check(game.State.banditsDefeated == 1 && game.State.gold == 47, "Combat gives XP and loot", ref checks);
            first.Hit(1000, Vector2.right);
            Check(game.State.banditsDefeated == 1 && game.State.gold == 47, "A corpse cannot give duplicate loot", ref checks);
            for (int i = 1; i < 5; i++) game.Enemies[i].Hit(1000, Vector2.right);
            Check(game.State.banditsDefeated == 5 && game.State.level == 3, "Combat levels player", ref checks);
            RpgEnemy boss = null;
            foreach (var enemy in game.Enemies) if (enemy.IsBoss) boss = enemy;
            Check(boss != null, "Boss unlocks after five kills", ref checks);
            game.Upgrade(true);
            Check(game.State.weaponLevel == 1 && game.State.gold == 55, "Merchant weapon upgrade", ref checks);
            game.BuyPotion();
            Check(game.State.potions == 4 && game.State.gold == 40, "Merchant potion purchase", ref checks);
            game.State.health = 30; game.DrinkPotion();
            Check(game.State.health == 90 && game.State.potions == 3, "Live potion use", ref checks);
            game.State.herbs = 2; game.CraftPotion();
            Check(game.State.herbs == 0 && game.State.potions == 4, "Alchemy recipe", ref checks);
            game.Heal();
            Check(game.State.health == game.State.MaxHealth && game.State.gold == 30, "Healer service", ref checks);
            boss.Hit(1000, Vector2.up);
            Check(game.State.bossDefeated && game.State.CanFinishQuest, "Boss completes objectives", ref checks);
            game.ClaimReward();
            Check(game.State.questStage == 2 && game.State.gold == 255 && game.Interface.IsModal, "Victory and reward screen", ref checks);
            game.ClaimReward();
            Check(game.State.gold == 255, "Victory reward cannot repeat", ref checks);
            game.Interface.Close();
            game.State.health = 0; game.Interface.ShowDeath(); game.Respawn();
            Check(game.State.health == game.State.MaxHealth && game.State.deaths == 1 && game.State.gold == 245, "Death and recovery", ref checks);
            game.Interface.ToggleInventory();
            Check(game.Interface.IsModal && game.BlockMovement, "Inventory blocks gameplay", ref checks);
            game.Interface.Close(); game.Interface.ShowMerchant();
            Check(game.Interface.IsModal, "Merchant dialog", ref checks);
            game.Interface.Close(); game.Interface.ShowHealer();
            Check(game.Interface.IsModal, "Healer dialog", ref checks);
            game.RestoreState(new RpgState());
            game.AcceptSideQuest(0);
            Check(game.State.herbsQuest == 1, "Mira quest acceptance", ref checks);
            game.State.herbs = 3; game.CompleteSideQuest(0);
            Check(game.State.herbsQuest == 1 && game.State.gold == 35, "Incomplete quest cannot pay out", ref checks);
            game.State.herbs = 4; game.CompleteSideQuest(0);
            Check(game.State.herbsQuest == 2 && game.State.herbs == 0 && game.State.gold == 60, "Mira turn-in and consumption", ref checks);
            game.AcceptSideQuest(1);
            game.State.collected.Add("supply1"); game.State.collected.Add("supply2");
            game.CompleteSideQuest(1);
            Check(game.State.suppliesQuest == 2 && game.State.gold == 120, "Mayor supply turn-in", ref checks);
            game.DeliverParcel();
            Check(!game.State.parcelDelivered, "Guard cannot accept an unassigned parcel", ref checks);
            game.AcceptSideQuest(2); game.DeliverParcel();
            Check(game.State.parcelDelivered && game.State.deliveryQuest == 1, "Guard delivery needs merchant turn-in", ref checks);
            game.CompleteSideQuest(2);
            Check(game.State.deliveryQuest == 2 && game.State.gold == 160, "Merchant delivery reward", ref checks);
            game.CompleteSideQuest(2);
            Check(game.State.gold == 160, "Repeated live turn-in is rejected", ref checks);
            game.Interface.ToggleJournal();
            Check(game.Interface.IsModal, "Journal with all side quests", ref checks);
            result = "PASS: " + checks + " live gameplay checks";
        }
        catch (Exception exception) { result = "FAIL after " + checks + " live checks: " + exception; Debug.LogError(result); }
        finally
        {
            game.RestoreState(JsonUtility.FromJson<RpgState>(snapshot));
            game.transform.position = originalPosition;
            game.Save(false);
        }
        string report = Path.Combine(Application.dataPath, "../Temp/RpgLiveValidation.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(report)); File.WriteAllText(report, result);
        Debug.Log(result);
    }
}
