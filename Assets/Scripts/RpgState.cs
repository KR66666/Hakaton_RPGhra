using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RpgState
{
    public int version = 1;
    public int currentArea;
    public int travelCompanion;
    public bool travelByCart;
    public List<int> completedAreas = new List<int>();
    public List<RpgAreaProgress> areaProgress = new List<RpgAreaProgress>();
    public bool AreaUnlocked(int area) => area >= 0 && area < RpgCampaign.Names.Length && (area == 0 || completedAreas.Contains(area - 1));
    public string TravelProblem(int area, int companion, bool cart)
    {
        if (!AreaUnlocked(area)) return "Nejprve dokonči předchozí úroveň.";
        if (health <= 0) return "Nejprve se nech oživit.";
        if (companion < 0 || companion > 2) return "Vyber platný doprovod.";
        if (companion == 1 && deliveryQuest != 2) return "Petr se přidá po splnění Zásilky pro strážného.";
        if (companion == 2 && herbsQuest != 2) return "Mira se přidá po splnění Bylin pro Miru.";
        if (cart && gold < 10) return "Na vůz potřebuješ 10 zlata.";
        return null;
    }
    public bool EnterArea(int area, int companion, bool cart, bool replay)
    {
        if (TravelProblem(area, companion, cart) != null || replay && !completedAreas.Contains(area)) return false;
        var previous = areaProgress.Find(p => p.area == currentArea);
        if (previous == null) { previous = new RpgAreaProgress { area = currentArea }; areaProgress.Add(previous); }
        previous.quest = questStage; previous.bandits = banditsDefeated; previous.boss = bossDefeated;
        previous.defeated = new List<string>(defeated);
        var target = areaProgress.Find(p => p.area == area);
        currentArea = area; travelCompanion = companion; travelByCart = cart;
        if (cart) gold -= 10;
        questStage = replay || target == null ? (area == 0 ? 0 : 1) : target.quest;
        banditsDefeated = replay || target == null ? 0 : target.bandits;
        bossDefeated = !replay && target != null && target.boss;
        defeated = replay || target == null ? new List<string>() : new List<string>(target.defeated);
        return true;
    }
    public bool heroIsBoy;
    public int level = 1;
    public int experience;
    public int gold = 35;
    public int potions = 3;
    public int herbs;
    public int weaponLevel;
    public int armorLevel;
    public int vitalityRank, strengthRank, agilityRank;
    public int UpgradePoints => Mathf.Max(0, level-1-vitalityRank-strengthRank-agilityRank);
    public bool UpgradeAttribute(int attribute)
    {
        if(UpgradePoints<=0 || attribute<0 || attribute>2)return false;
        int rank=attribute==0?vitalityRank:attribute==1?strengthRank:agilityRank;
        if(rank>=5)return false;
        if(attribute==0){vitalityRank++;health=Mathf.Min(MaxHealth,health+15);}
        else if(attribute==1)strengthRank++;else agilityRank++;
        return true;
    }
    public float health = 100;
    public float stamina = 100;
    public float x;
    public float y;
    public int trackedQuest = -1;
    public int questStage;
    public int banditsDefeated;
    public bool bossDefeated;
    public int deaths;
    public int herbsQuest;
    public int suppliesQuest;
    public int deliveryQuest;
    public bool parcelDelivered;
    public long gardenReadyAt;
    public List<string> collected = new List<string>();
    public List<string> defeated = new List<string>();

    public int MaxHealth => 100 + (level - 1) * 20 + vitalityRank*15;
    public int Damage => 18 + (level - 1) * 4 + weaponLevel * 9 + strengthRank*3;
    public int Defense => armorLevel * 3;
    public int NextLevelExperience => 50 + (level - 1) * 40;
    public bool CanFinishQuest => questStage == 1 && banditsDefeated >= 5 && bossDefeated;

    public bool AddExperience(int amount)
    {
        experience += Mathf.Max(0, amount);
        bool leveled = false;
        while (experience >= NextLevelExperience && level < 30)
        {
            experience -= NextLevelExperience;
            level++;
            health = MaxHealth;
            stamina = 100;
            leveled = true;
        }
        return leveled;
    }

    public bool DrinkPotion()
    {
        if (potions <= 0 || health >= MaxHealth || health <= 0)
            return false;
        potions--;
        health = Mathf.Min(MaxHealth, health + 60);
        return true;
    }

    public bool Spend(int amount)
    {
        if (amount < 0 || gold < amount)
            return false;
        gold -= amount;
        return true;
    }

    public bool AcceptQuest()
    {
        if (questStage != 0)
            return false;
        questStage = 1;
        return true;
    }

    public bool FinishQuest()
    {
        if (!CanFinishQuest)
            return false;
        questStage = 2;
        gold += 150;
        potions += 3;
        AddExperience(150);
        return true;
    }

    public int SideQuestStage(int id) => id == 0 ? herbsQuest : id == 1 ? suppliesQuest : deliveryQuest;

    public bool AcceptSideQuest(int id)
    {
        if (id < 0 || id > 2 || SideQuestStage(id) != 0) return false;
        SetSideQuestStage(id, 1);
        return true;
    }

    public int SuppliesFound => (collected.Contains("supply1") ? 1 : 0) + (collected.Contains("supply2") ? 1 : 0);
    public bool SideQuestReady(int id)
    {
        if (id < 0 || id > 2 || SideQuestStage(id) != 1) return false;
        return id == 0 ? herbs >= 4 : id == 1 ? SuppliesFound >= 2 : parcelDelivered;
    }

    public bool FinishSideQuest(int id)
    {
        if (!SideQuestReady(id)) return false;
        if (id == 0) { herbs -= 4; gold += 25; potions += 2; AddExperience(45); }
        else if (id == 1) { gold += 60; AddExperience(60); }
        else { gold += 40; potions++; AddExperience(35); }
        SetSideQuestStage(id, 2);
        return true;
    }

    private void SetSideQuestStage(int id, int stage)
    {
        if (id == 0) herbsQuest = stage;
        else if (id == 1) suppliesQuest = stage;
        else deliveryQuest = stage;
    }

    public void Normalize()
    {
        currentArea = Mathf.Clamp(currentArea, 0, RpgCampaign.Names.Length - 1);
        completedAreas = completedAreas ?? new List<int>();
        completedAreas.RemoveAll(a => a < 0 || a >= RpgCampaign.Names.Length);
        areaProgress = areaProgress ?? new List<RpgAreaProgress>();
        areaProgress.RemoveAll(p => p == null);
        foreach (var progress in areaProgress) progress.defeated = progress.defeated ?? new List<string>();
        if (questStage == 2 && !completedAreas.Contains(currentArea)) completedAreas.Add(currentArea);
        level = Mathf.Clamp(level, 1, 30);
        vitalityRank=Mathf.Clamp(vitalityRank,0,Mathf.Min(5,level-1));
        strengthRank=Mathf.Clamp(strengthRank,0,Mathf.Min(5,level-1-vitalityRank));
        agilityRank=Mathf.Clamp(agilityRank,0,Mathf.Min(5,level-1-vitalityRank-strengthRank));
        experience = Mathf.Clamp(experience, 0, 100000);
        gold = Mathf.Clamp(gold, 0, 1000000);
        potions = Mathf.Clamp(potions, 0, 999);
        herbs = Mathf.Clamp(herbs, 0, 999);
        weaponLevel = Mathf.Clamp(weaponLevel, 0, 5);
        armorLevel = Mathf.Clamp(armorLevel, 0, 5);
        trackedQuest=Mathf.Clamp(trackedQuest,-1,2);
        questStage = Mathf.Clamp(questStage, 0, 2);
        herbsQuest = Mathf.Clamp(herbsQuest, 0, 2);
        suppliesQuest = Mathf.Clamp(suppliesQuest, 0, 2);
        deliveryQuest = Mathf.Clamp(deliveryQuest, 0, 2);
        banditsDefeated = Mathf.Max(0, banditsDefeated);
        deaths = Mathf.Max(0, deaths);
        if (float.IsNaN(health) || float.IsInfinity(health)) health = MaxHealth;
        if (float.IsNaN(stamina) || float.IsInfinity(stamina)) stamina = 100;
        health = Mathf.Clamp(health, 0, MaxHealth);
        stamina = Mathf.Clamp(stamina, 0, 100);
        if (float.IsNaN(x) || float.IsInfinity(x)) x = 0;
        if (float.IsNaN(y) || float.IsInfinity(y)) y = 0;
        collected = collected ?? new List<string>();
        defeated = defeated ?? new List<string>();
    }
}



// Roof footprints measured against the 1536 x 1024 overhead village artwork.
// Movement uses a small circle at the character's feet rather than the sprite's transparent border.
public static class RpgWorldGeometry
{
    // A directly overhead character is anchored at its ground-plane centre.
    public const float FeetOffset = 0f;
    private struct Road
    {
        public Vector2 a, b;
        public float radius;
        public Road(float x1, float y1, float x2, float y2, float width)
        { a = new Vector2(x1, y1); b = new Vector2(x2, y2); radius = width; }
    }

    // Whitelist of paved surfaces, traced in the background's pixel coordinates.
    // Rectangular roof blockers below take priority over this road network.
    private static readonly Vector2[] Square =
    {
        new Vector2(651, 432), new Vector2(916, 405), new Vector2(980, 470),
        new Vector2(1000, 675), new Vector2(900, 697), new Vector2(840, 710),
        new Vector2(688, 710), new Vector2(630, 685), new Vector2(614, 636), new Vector2(628, 455)
    };
    private static readonly Road[] Roads =
    {
        new Road(750,440,744,402,20), // Existing shop doorstep below the north-square house.
        new Road(650,450,535,448,22), new Road(535,448,420,466,20),
        new Road(420,466,282,428,18), new Road(282,428,210,403,17),
        new Road(210,403,150,420,17), new Road(150,420,108,420,16),
        new Road(108,420,108,466,15), new Road(108,466,106,535,15),
        new Road(106,535,145,570,15),
        new Road(145,570,165,625,15), new Road(165,625,268,655,18),
        new Road(268,655,292,683,16), new Road(292,683,276,772,15),
        new Road(276,772,276,858,15), new Road(276,858,205,850,15),
        new Road(205,850,145,842,14), new Road(145,842,152,905,14),
        new Road(152,905,152,1015,14),
        new Road(210,403,224,320,14), new Road(224,320,240,260,14),
        new Road(240,260,224,190,14), new Road(224,190,205,190,14),
        new Road(205,190,205,90,14), new Road(205,90,240,0,14),
        new Road(224,320,280,289,13), new Road(280,289,370,285,13),
        new Road(370,285,400,266,13), new Road(400,266,400,180,13),
        new Road(400,180,384,172,12), new Road(384,172,384,120,11),
        new Road(384,120,384,10,11),
        new Road(110,466,108,300,12), new Road(108,300,125,245,12),
        new Road(535,448,535,285,15), new Road(535,285,565,285,15), new Road(565,285,565,200,14),
        new Road(565,200,550,170,14), new Road(550,170,550,100,14), new Road(550,100,550,10,14),
        new Road(900,420,915,300,16), new Road(915,300,908,275,16),
        new Road(915,300,810,300,16), new Road(810,300,670,282,14),
        new Road(670,282,570,280,14), new Road(570,280,535,285,14),
        new Road(980,500,1075,523,21), new Road(1075,523,1235,530,20),
        new Road(1235,530,1300,505,20), new Road(1300,505,1350,477,20),
        new Road(1350,477,1500,477,20), new Road(1500,477,1536,477,18),
        new Road(1300,505,1350,550,14), new Road(1350,550,1355,675,13),
        new Road(1355,675,1335,740,13), new Road(1335,740,1340,930,13),
        new Road(1340,930,1360,1024,13),
        new Road(975,650,1010,720,18), new Road(1010,720,1065,770,16),
        new Road(1065,770,1050,840,14), new Road(1050,840,1055,950,12),
        new Road(1055,950,1060,1024,12), new Road(1065,770,1190,720,15),
        new Road(1190,720,1240,700,14), new Road(1240,700,1290,690,14), new Road(1290,690,1335,740,13),
        new Road(640,680,560,705,15), new Road(560,705,560,835,12),
        new Road(560,835,620,860,14), new Road(620,860,680,900,15),
        new Road(680,900,710,1015,15),
        // Dirt lane below the mill and its connection to the western village.
        new Road(205,190,205,220,14), new Road(205,220,120,220,14),
        new Road(120,220,15,220,14), new Road(125,245,120,220,14),
        // Northern and eastern residential lanes, outside the castle walls.
        new Road(1075,523,1092,420,12), new Road(1092,420,1092,330,12),
        new Road(1092,330,1070,250,14), new Road(1070,250,1090,170,14),
        new Road(1090,170,1170,140,14), new Road(1170,140,1240,170,14),
        new Road(1240,170,1290,175,14), new Road(1290,175,1300,230,14), new Road(1300,230,1290,285,13),
        new Road(1290,285,1290,304,12), new Road(1290,304,1210,304,12),
        // Lanes around the southern houses connect both sides of the village.
        new Road(276,858,335,855,14), new Road(335,855,460,855,14),
        new Road(460,855,560,835,14), new Road(680,900,770,860,13),
        new Road(770,860,920,850,13), new Road(920,850,920,985,13),
        new Road(920,985,1055,985,13)
    };
    private static readonly Rect[] MarketStalls =
    {
        new Rect(574,400,61,30), new Rect(579,481,58,142),
        new Rect(782,335,57,44), new Rect(842,334,34,57),
        new Rect(952,370,33,58), new Rect(989,466,81,31),
        new Rect(1009,545,47,140), new Rect(886,669,61,32),
        new Rect(786,696,57,29), new Rect(713,699,39,34)
    };
    private static readonly Rect[] Buildings =
    {
        new Rect(83,108,93,92), // Mill body; the lane passes around its southern side.
        new Rect(276, 306, 207, 112), new Rect(454, 172, 88, 104),
        new Rect(394, 30, 107, 130), new Rect(284, 50, 91, 57),
        new Rect(550, 295, 125, 101), new Rect(688, 314, 88, 73),
        new Rect(988, 325, 91, 137), new Rect(1109, 438, 116, 84),
        new Rect(1086, 546, 102, 84), new Rect(1114, 632, 104, 71),
        new Rect(1216, 569, 111, 93), new Rect(1248, 310, 104, 104),
        new Rect(1106, 278, 77, 112), new Rect(1191, 194, 77, 103),
        new Rect(1119, 45, 80, 62), new Rect(1437, 60, 67, 82),
        new Rect(1070, 813, 191, 143), new Rect(935, 829, 102, 148),
        new Rect(793, 706, 101, 122), new Rect(695, 732, 97, 87),
        new Rect(575, 723, 108, 105), new Rect(324, 690, 219, 144),
        new Rect(471, 478, 107, 141), new Rect(293, 492, 100, 116),
        new Rect(229, 447, 102, 79), new Rect(164, 490, 78, 72),
        new Rect(121, 438, 73, 85), new Rect(32, 346, 61, 102),
        new Rect(68, 581, 77, 83), new Rect(24, 819, 99, 137),
        new Rect(198, 879, 78, 115), new Rect(311, 875, 143, 94),
        new Rect(836, 882, 61, 96), new Rect(1219, 718, 78, 98),
        new Rect(702, 54, 209, 86), new Rect(834, 104, 130, 105)
    };

    public static Vector3 Move(Vector3 current, Vector3 desired, Bounds map, float feetOffset = FeetOffset)
    {
        var result = current;
        // Sweep the full movement, including sprint/roll frames; never just test its endpoint.
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(current, desired) / 0.06f));
        var delta = (desired - current) / steps;
        for (int i = 0; i < steps; i++)
        {
            var horizontal = result + new Vector3(delta.x, 0, 0);
            if (!Blocked(horizontal, map, feetOffset)) result.x = horizontal.x;
            var vertical = result + new Vector3(0, delta.y, 0);
            if (!Blocked(vertical, map, feetOffset)) result.y = vertical.y;
        }
        return result;
    }

    public static bool Blocked(Vector3 position, Bounds map, float feetOffset)
    {
        var point = new Vector2(
            (position.x - map.min.x) / map.size.x * 1536,
            (map.max.y - position.y - feetOffset) / map.size.y * 1024);
        return !WalkablePixel(point) || !WalkablePixel(point + new Vector2(3, 0)) ||
            !WalkablePixel(point - new Vector2(3, 0)) || !WalkablePixel(point + new Vector2(0, 3)) ||
            !WalkablePixel(point - new Vector2(0, 3));
    }

    private static bool WalkablePixel(Vector2 point)
    {
        if (point.x < 5 || point.x > 1531 || point.y < 5 || point.y > 1019) return false;
        foreach (var building in Buildings)
        {
            var padded = new Rect(building.xMin - 2, building.yMin - 2, building.width + 4, building.height + 4);
            if (padded.Contains(point)) return false;
        }
        foreach (var stall in MarketStalls) if (stall.Contains(point)) return false;
        bool inside = false;
        for (int i = 0, j = Square.Length - 1; i < Square.Length; j = i++)
            if ((Square[i].y > point.y) != (Square[j].y > point.y) &&
                point.x < (Square[j].x - Square[i].x) * (point.y - Square[i].y) / (Square[j].y - Square[i].y) + Square[i].x)
                inside = !inside;
        if (inside) return true;
        foreach (var road in Roads)
        {
            var segment = road.b - road.a;
            float t = Mathf.Clamp01(Vector2.Dot(point - road.a, segment) / segment.sqrMagnitude);
            if ((point - (road.a + segment * t)).sqrMagnitude <= road.radius * road.radius) return true;
        }
        return false;
    }

    public static Vector3 PixelPosition(Vector2 point, Bounds map, float feetOffset = FeetOffset)
    {
        return new Vector3(map.min.x + point.x / 1536 * map.size.x,
            map.max.y - point.y / 1024 * map.size.y - feetOffset, map.center.z);
    }

    public static Vector3 NearestWalkable(Vector3 position, Bounds map)
    {
        if (!Blocked(position, map, FeetOffset)) return position;
        for (float radius = 0.1f; radius <= 10; radius += 0.1f)
            for (int angle = 0; angle < 32; angle++)
            {
                float radians = angle * Mathf.PI / 16;
                var candidate = position + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0) * radius;
                if (!Blocked(candidate, map, FeetOffset)) return candidate;
            }
        var fallback = PixelPosition(new Vector2(768, 600), map);
        fallback.z = position.z;
        return fallback;
    }
}

