using System;
using System.Collections;
using UnityEngine;

public sealed partial class RpgState
{
    public bool swordTrained,bowTrained,hasBow,cellarCandlesTaken,forestCandleTaken,libraryCandleTaken;
    public bool finalRitualStarted,storyComplete;
    public int finalEnemiesRemaining;
    public const int FinalWaveSize=8;
    public int RequiredCandles => cycle+2;
    public string RitualShape => cycle==1?"trojúhelník":cycle==2?"čtverec":"hvězda";
    public int CyclePointBudget => cycle==1?1:cycle==2?3:4;
    public int CyclePointsAwarded => rewardedQuests.FindAll(key=>key.StartsWith(cycle+":" )).Count;
    public bool CanSwordFight => swordTrained && hasIronSword;
    public bool CanBowFight => bowTrained && hasBow;
    public bool CanFight(bool flying) => flying?CanBowFight:CanSwordFight||CanBowFight;
    public bool LearnCombat(bool bow,bool paid)
    {
        if(cycle<(bow?3:2) || (bow?bowTrained:swordTrained) || health<=0)return false;
        if(paid && !Spend(30))return false;
        if(bow)bowTrained=true;else swordTrained=true;
        return true;
    }
    public bool BuyBow()
    {
        if(cycle<3 || hasBow || !Spend(40))return false;
        hasBow=true;return true;
    }
    public bool CollectExtraCandle(bool library)
    {
        if(cycle<2 || health<=0 || (library?libraryCandleTaken:forestCandleTaken))return false;
        if(library)libraryCandleTaken=true;else forestCandleTaken=true;
        candles++;RewardQuest(library?"library-candle":"forest-candle");RecordOwnedArchiveItems();return true;
    }
    public bool StartFinalWave()
    {
        if(cycle!=3 || !CanPerformRitual || finalRitualStarted)return false;
        finalRitualStarted=true;finalEnemiesRemaining=FinalWaveSize;return true;
    }
    public bool FinishFinalEnemy()
    {
        if(!finalRitualStarted || storyComplete || finalEnemiesRemaining<=0)return false;
        finalEnemiesRemaining--;
        if(finalEnemiesRemaining==0){storyComplete=true;RewardQuest("escape");}
        return true;
    }
}

public sealed partial class RpgGame
{
    public void LearnCombat(bool bow,bool paid)
    {
        bool teacher=DialogueNpcId=="guard"&&!State.NpcDefeated("guard") ||
            bow&&DialogueNpcId=="districtRybář Oldřich"&&!State.NpcDefeated(DialogueNpcId) ||
            !bow&&paid&&DialogueNpcId=="districtKovář Mikuláš"&&!State.NpcDefeated(DialogueNpcId);
        if(!teacher || !State.LearnCombat(bow,paid)){Notify("Výcvik nyní není dostupný. Placený výcvik stojí 30 zlata.");return;}
        Save(false);Notify(bow?"Naučil ses bojovat s lukem. Znalost ti zůstane do dalších cyklů.":"Naučil ses bojovat s mečem. Znalost ti zůstane do dalších cyklů.");
    }
    public void BuyBow()
    {
        if(DialogueNpcId!="districtRybář Oldřich" || State.NpcDefeated(DialogueNpcId) || !State.BuyBow()){Notify("Na luk potřebuješ 40 zlata.");return;}
        Save(false);Notify("Koupen luk.");
    }
    private void BuildCycleExploration()
    {
        if(State.cycle<2)return;
        if(State.place==RpgPlace.Forest && !State.forestCandleTaken)
            AddPickup("forestCandle","Svíčka v lese","extraCandle",new Vector2(1,5),Color.yellow);
        if(State.place==RpgPlace.DarkForest)
        {
            SpawnEnemy("flying0",new Vector2(0,3),false);
            SpawnEnemy("wolf1",new Vector2(2,-3),false);
        }
    }
    private void CollectCycleCandle(bool library)
    {
        if(!(library?State.TakeLibraryCandle():State.CollectExtraCandle(false)))return;
        BuildWorld();Save(false);Interface.Close();Notify("Svíčka získána • celkem "+State.candles+"/"+State.RequiredCandles);
    }
}

public sealed partial class RpgInterface
{
    private void ShowExistingHunter()
    {
        var s=game.State;
        Open("resident","RYBÁŘ OLDŘICH",s.cycle<3?"Rybařím a také lovím. S lukem ti poradím, až ho budeš potřebovat.":"Na létající tvory potřebuješ luk. Mohu tě s ním naučit zacházet.");
        if(s.cycle>=3 && !s.bowTrained)Action("Naučit se boj s lukem • zdarma",()=>{game.LearnCombat(true,false);ShowExistingHunter();});
        if(s.cycle>=3 && !s.hasBow)Action("Koupit luk • 40 zl.",()=>{game.BuyBow();ShowExistingHunter();});
        NpcChallenge(game.DialogueNpcId);Action("Odejít",Close);
    }
}
