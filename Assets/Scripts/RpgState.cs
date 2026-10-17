using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RpgState
{
    public int version = 1;
    public int level = 1;
    public int experience;
    public int gold = 35;
    public int potions = 3;
    public int herbs;
    public int weaponLevel;
    public int armorLevel;
    public float health = 100;
    public float stamina = 100;
    public float x;
    public float y;
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

    public int MaxHealth => 100 + (level - 1) * 20;
    public int Damage => 18 + (level - 1) * 4 + weaponLevel * 9;
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
        level = Mathf.Clamp(level, 1, 30);
        experience = Mathf.Clamp(experience, 0, 100000);
        gold = Mathf.Clamp(gold, 0, 1000000);
        potions = Mathf.Clamp(potions, 0, 999);
        herbs = Mathf.Clamp(herbs, 0, 999);
        weaponLevel = Mathf.Clamp(weaponLevel, 0, 5);
        armorLevel = Mathf.Clamp(armorLevel, 0, 5);
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
    private static readonly Rect[] Buildings =
    {
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

    public static Vector3 Move(Vector3 current, Vector3 desired, Bounds map, float feetOffset = -0.45f)
    {
        var result = current;
        var horizontal = new Vector3(desired.x, current.y, current.z);
        if (!Blocked(horizontal, map, feetOffset)) result.x = desired.x;
        var vertical = new Vector3(result.x, desired.y, current.z);
        if (!Blocked(vertical, map, feetOffset)) result.y = desired.y;
        return result;
    }

    public static bool Blocked(Vector3 position, Bounds map, float feetOffset)
    {
        var point = new Vector2(
            (position.x - map.min.x) / map.size.x * 1536,
            (map.max.y - position.y - feetOffset) / map.size.y * 1024);
        foreach (var building in Buildings)
        {
            var padded = new Rect(building.xMin - 5, building.yMin - 5, building.width + 10, building.height + 10);
            if (padded.Contains(point)) return true;
        }
        return false;
    }
}

