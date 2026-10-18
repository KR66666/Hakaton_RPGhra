using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RpgState
{
    public int version = 1;
    public RpgPlace place = RpgPlace.Town;
    // 0 guide, 1 elder, 2 shop, 3 invitation, 4 cellar, 6 candles, 7 ritual instruction.
    public int cellarQuest;
    public int candleStoryVersion = 1;
    public int cycle = 1;
    public const int FirstCycleCandles = 3;
    public int candles;
    public List<string> defeatedNpcs = new List<string>();
    public bool NpcDefeated(string id) => defeatedNpcs.Contains(id);
    public bool DefeatNpc(string id)
    {
        if(string.IsNullOrEmpty(id) || NpcDefeated(id))return false;
        defeatedNpcs.Add(id);ResolveNpcClues();return true;
    }
    public void ResolveNpcClues()
    {
        if(cycle!=1)return;
        if(NpcDefeated("guard") && cellarQuest==0)cellarQuest=1;
        if(NpcDefeated("elder") && cellarQuest==1)cellarQuest=2;
        if(NpcDefeated("merchant") && cellarQuest==2)cellarQuest=3;
    }
    public bool ritualKnown;
    public bool ritualFailed;
    public bool CollectCandles(bool rush)
    {
        if(cycle!=1 || health<=0 || cellarQuest<3 || cellarQuest>4 || candles!=0)return false;
        candles=FirstCycleCandles;cellarQuest=6;
        if(rush)health=Mathf.Max(1,health-20);
        return true;
    }
    public bool ExplainRitual()
    {
        if(cycle!=1 || cellarQuest!=6 || candles!=FirstCycleCandles || ritualKnown)return false;
        ritualKnown=true;cellarQuest=7;RewardQuest("candles");return true;
    }
    public bool CanPerformRitual => cycle==1 && place==RpgPlace.ChurchRuins && ritualKnown && candles==FirstCycleCandles && health>0;
    public RpgState BeginSecondCycle()
    {
        if(!CanPerformRitual)return null;
        var next=CreateNew(heroIsBoy);
        next.cycle=2;next.ritualFailed=true;next.deaths=deaths+1;
        next.upgradePoints=upgradePoints;next.unlockedSkills=new List<int>(unlockedSkills);
        next.rewardedQuests=new List<string>(rewardedQuests);next.health=next.MaxHealth;
        return next;
    }
    public int guideSeenStage = -1;
    public bool NeedsGuideInstruction => !NpcDefeated("guard") && cycle==1 && currentArea==0 && guideSeenStage!=cellarQuest &&
        (cellarQuest==0 || cellarQuest==1 || cellarQuest==2 || cellarQuest==6);
    public bool cellarNoteTaken, cellarNoteOpened;
    public string CellarObjective => cycle==2 ? "Druhý cyklus začal. Pamatuješ si neúspěšný rituál. Prozkoumej svět." : cellarQuest == 0 ? "Promluv s Petrem Křížem u mostu." :
        cellarQuest == 1 ? "Zeptej se dědy Matěje na náměstí na svíčky." :
        cellarQuest < 4 ? (NpcDefeated("merchant")?"Prozkoumej opuštěný obchod a sejdi do sklepa.":"Zeptej se obchodnice Elišky na sklep.") :
        cellarQuest < 6 ? "Ve sklepě získej tři svíčky. Můžeš také utéct a vrátit se." :
        cellarQuest == 6 ? (NpcDefeated("guard")?"Přečti průvodcovy poznámky v mobilu [Q].":"Vrať se k průvodci a řekni mu, že máš svíčky.") : "Dojdi do zříceniny kostela a proveď rituál u trojúhelníku [E].";
    public int currentArea;
    public int travelCompanion;
    public bool travelByCart;
    public List<int> completedAreas = new List<int>();
    public List<RpgAreaProgress> areaProgress = new List<RpgAreaProgress>();
    public bool AreaUnlocked(int area) => area >= 0 && area < RpgCampaign.Names.Length && (area == 0 || completedAreas.Contains(area - 1));
    public string TravelProblem(int area, int companion, bool cart)
    {
        if (!AreaUnlocked(area)) return "Nejprve dokonči předchozí oblast.";
        if (health <= 0) return "Nejprve se nech oživit.";
        if (companion < 0 || companion > 2) return "Vyber platný doprovod.";
        if (companion == 1 && deliveryQuest != 2 && cellarQuest == 0) return "Petr se přidá po splnění Zásilky pro strážného.";
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
        currentArea = area; travelCompanion = companion==1?0:companion; travelByCart = cart;
        if (cart) gold -= 10;
        questStage = replay || target == null ? (area == 0 ? 0 : 1) : target.quest;
        banditsDefeated = replay || target == null ? 0 : target.bandits;
        bossDefeated = !replay && target != null && target.boss;
        defeated = replay || target == null ? new List<string>() : new List<string>(target.defeated);
        return true;
    }
    public bool heroIsBoy;
    // Appearance is independent of gameplay attributes.
    public static RpgState CreateNew(bool boy)
    {
        var state=new RpgState { heroIsBoy=boy };
        state.Normalize();state.health=state.MaxHealth;state.stamina=state.MaxStamina;
        return state;
    }
    // Legacy save fields; no longer used for progression.
    public int level = 1;
    public int experience;
    public int gold = 35;
    public int potions = 3;
    public int herbs;
    public int weaponLevel;
    public int armorLevel;
    public int vitalityRank, strengthRank, agilityRank;
    public int skillSystemVersion;
    public int upgradePoints;
    public List<int> unlockedSkills = new List<int>();
    public List<string> rewardedQuests = new List<string>();
    public int UpgradePoints => upgradePoints;
    public bool HasSkill(int id) => unlockedSkills.Contains(id);
    public float DodgeChance => HasSkill(2)?.15f:0;
    public float InstantWinChance => HasSkill(3)?.10f:0;
    public bool RewardQuest(string id)
    {
        string key=cycle+":"+id;
        if(rewardedQuests.Contains(key))return false;
        rewardedQuests.Add(key);upgradePoints++;return true;
    }
    public bool UpgradeAttribute(int attribute)
    {
        if(upgradePoints<=0 || attribute<0 || attribute>3 || HasSkill(attribute))return false;
        upgradePoints--;unlockedSkills.Add(attribute);
        if(attribute==0)health=Mathf.Min(MaxHealth,health+20);
        return true;
    }
    public float health = 100;
    public float stamina = 100;
    public float x = .5f;
    public float y = 3.5f;
    public int trackedQuest = -1;
    public int questStage;
    public int banditsDefeated;
    public bool bossDefeated;
    public int deaths;
    public int herbsQuest;
    public int suppliesQuest;
    public int deliveryQuest;
    public List<int> knownSideQuests = new List<int>();
    public bool KnowsSideQuest(int id) => id>=0 && id<=2 && (SideQuestStage(id)>0 || knownSideQuests.Contains(id));
    public bool DiscoverSideQuest(int id)
    {
        if(id<0 || id>2 || KnowsSideQuest(id))return false;
        knownSideQuests.Add(id);return true;
    }
    public bool parcelDelivered;
    public long gardenReadyAt;
    public List<string> collected = new List<string>();
    public List<string> defeated = new List<string>();

    public int MaxHealth => 100 + (HasSkill(0)?20:0);
    public int MaxStamina => 100;
    public int Damage => 18 + weaponLevel * 9 + (HasSkill(1)?6:0);
    public int Defense => armorLevel * 3;
    public bool CanFinishQuest => questStage == 1 && banditsDefeated >= 5 && bossDefeated;

    public bool AddExperience(int amount) => false; // Compatibility for older editor tools.

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
        RewardQuest("main:"+currentArea);
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
        if (id == 0) { herbs -= 4; gold += 25; potions += 2; }
        else if (id == 1) { gold += 60; }
        else { gold += 40; potions++; }
        SetSideQuestStage(id, 2);RewardQuest("side:"+id);
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
        defeatedNpcs=defeatedNpcs ?? new List<string>();
        defeatedNpcs.RemoveAll(string.IsNullOrEmpty);
        defeatedNpcs=new List<string>(new HashSet<string>(defeatedNpcs));
        cycle=Mathf.Clamp(cycle,1,2);
        ResolveNpcClues();
        currentArea=Mathf.Clamp(currentArea,0,RpgCampaign.Names.Length-1);
        unlockedSkills=unlockedSkills ?? new List<int>();
        unlockedSkills.RemoveAll(id=>id<0 || id>3);
        unlockedSkills=new List<int>(new HashSet<int>(unlockedSkills));
        rewardedQuests=rewardedQuests ?? new List<string>();
        if(skillSystemVersion==0)
        {
            if(vitalityRank>0 && !HasSkill(0))unlockedSkills.Add(0);
            if(strengthRank>0 && !HasSkill(1))unlockedSkills.Add(1);
            if(agilityRank>0 && !HasSkill(2))unlockedSkills.Add(2);
            for(int id=0;id<3;id++)if(SideQuestStage(id)==2)RewardQuest("side:"+id);
            if(questStage==2)RewardQuest("main:"+currentArea);
            if(cellarQuest==7 && ritualKnown)RewardQuest("candles");
            skillSystemVersion=1;
        }
        upgradePoints=Mathf.Clamp(upgradePoints,0,100000);
        knownSideQuests=knownSideQuests ?? new List<int>();
        knownSideQuests.RemoveAll(id=>id<0 || id>2);
        // Old note saves rejoin the candle branch without keeping the obsolete reward ending.
        if(candleStoryVersion==0 || (cellarQuest>=6 && candles==0 && !ritualFailed))
        {
            if(cellarQuest>=6){candles=FirstCycleCandles;cellarQuest=6;ritualKnown=false;}
            candleStoryVersion=1;
        }
        if(cellarQuest==5)cellarQuest=4;
        cycle=Mathf.Clamp(cycle,1,2);
        candles=candles>=FirstCycleCandles?FirstCycleCandles:0;
        place = RpgLocations.Normalize(place);
        currentArea = Mathf.Clamp(currentArea, 0, RpgCampaign.Names.Length - 1);
        completedAreas = completedAreas ?? new List<int>();
        completedAreas.RemoveAll(a => a < 0 || a >= RpgCampaign.Names.Length);
        areaProgress = areaProgress ?? new List<RpgAreaProgress>();
        areaProgress.RemoveAll(p => p == null);
        foreach (var progress in areaProgress) progress.defeated = progress.defeated ?? new List<string>();
        if (questStage == 2 && !completedAreas.Contains(currentArea)) completedAreas.Add(currentArea);
        level=1;experience=0;vitalityRank=strengthRank=agilityRank=0;
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
    public const float LegacyScale = .58f;
    public static readonly Vector2 ExpandedCoreOffset = new Vector2(358,163);
    public static Vector2 ToExpandedPixel(Vector2 original) => original*LegacyScale+ExpandedCoreOffset;
    public static Vector2 FromExpandedPixel(Vector2 expanded) => (expanded-ExpandedCoreOffset)/LegacyScale;
    public static readonly Vector2 ShopDoorPixel=FromExpandedPixel(new Vector2(655,345));
    public static readonly Vector2 ShopApproachPixel=FromExpandedPixel(new Vector2(650,395));
    public static readonly Vector2 ChurchApproachPixel=FromExpandedPixel(new Vector2(1045,395));
    public static readonly Vector2 LibraryApproachPixel=FromExpandedPixel(new Vector2(650,650));
    public static Bounds ExpandedBounds(Bounds original)
    {
        var size=new Vector3(original.size.x/LegacyScale,original.size.y/LegacyScale,original.size.z);
        var centerPixel=ToExpandedPixel(new Vector2(768,512));
        var shift=new Vector3((centerPixel.x/1536-.5f)*size.x,(.5f-centerPixel.y/1024)*size.y,0);
        return new Bounds(original.center-shift,size);
    }

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

    public static bool Blocked(Vector3 position,Bounds map,float feetOffset)
    {
        var point=new Vector2((position.x-map.min.x)/map.size.x*1536,
            (map.max.y-position.y-feetOffset)/map.size.y*1024);
        const float margin=1.74f;
        return !WalkableExpandedPixel(point)||!WalkableExpandedPixel(point+new Vector2(margin,0))||
            !WalkableExpandedPixel(point-new Vector2(margin,0))||!WalkableExpandedPixel(point+new Vector2(0,margin))||
            !WalkableExpandedPixel(point-new Vector2(0,margin));
    }
    private static bool WalkableExpandedPixel(Vector2 point) => point.x>=4&&point.x<=1532&&point.y>=4&&point.y<=1020&&RpgTownExpansion.Walkable(point);
    // Legacy quest/NPC pixel coordinates stay anchored to the old 40-unit central village.
    public static Vector3 PixelPosition(Vector2 point,Bounds map,float feetOffset=FeetOffset) =>
        ExpandedPixelPosition(ToExpandedPixel(point),map,feetOffset);
    public static Vector3 ExpandedPixelPosition(Vector2 point,Bounds map,float feetOffset=FeetOffset)
    {
        return new Vector3(map.min.x+point.x/1536*map.size.x,
            map.max.y-point.y/1024*map.size.y-feetOffset,map.center.z);
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
        var fallback = PixelPosition(new Vector2(730, 550), map);
        fallback.z = position.z;
        return fallback;
    }
}

