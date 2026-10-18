using UnityEngine;
using System.Collections;

public enum RpgBattleAction { Attack, Guard, Potion, Escape, Bow }
public enum RpgBattleResult { Ongoing, Victory, Defeat, Escaped }

// A round advances only when the player chooses a valid action.
public sealed class RpgTurnBattle
{
    public readonly RpgState Player;
    public readonly bool Boss,Flying;
    public readonly string OpponentName;
    public int EnemyHealth { get; private set; }
    public int Round { get; private set; } = 1;
    public RpgBattleResult Result { get; private set; }
    public string Message { get; private set; } = "Jsi na tahu. Vyber akci.";
    public RpgTurnBattle(RpgState player,int enemyHealth,bool boss,string name=null,bool flying=false)
    {Player=player;EnemyHealth=enemyHealth;Boss=boss;Flying=flying;OpponentName=name??RpgEnemy.KindName(flying);}
    public bool Act(RpgBattleAction action,float instantRoll,float dodgeRoll,float escapeRoll)
    {
        if(Result!=RpgBattleResult.Ongoing)return false;
        if(!Player.CanFight(Flying) && action!=RpgBattleAction.Escape){Message="Chybí ti výcvik nebo vhodná zbraň. Můžeš pouze utéct.";return false;}
        bool guard=false;
        switch(action)
        {
            case RpgBattleAction.Attack:
            case RpgBattleAction.Bow:
                if(action==RpgBattleAction.Attack && (!Player.CanSwordFight || Flying) || action==RpgBattleAction.Bow && !Player.CanBowFight){Message="Tuto zbraň nemůžeš použít.";return false;}
                bool instant=!Boss && instantRoll<Player.InstantWinChance;
                int hit=instant?EnemyHealth:action==RpgBattleAction.Bow?16+(Player.HasSkill(1)?6:0):Player.Damage;
                EnemyHealth=Mathf.Max(0,EnemyHealth-hit);
                Message=instant?"Rozhodující úder! Nepřítel je okamžitě poražen.":"Útok: −"+hit+" zdraví nepřítele.";break;
            case RpgBattleAction.Guard:guard=true;Message="Kryješ se. Příští zásah bude slabší.";break;
            case RpgBattleAction.Potion:
                if(!Player.DrinkPotion()){Message="Lektvar nyní nelze použít. Vyber jinou akci.";return false;}
                Message="Používáš léčivý lektvar.";break;
            case RpgBattleAction.Escape:
                if(escapeRoll<.5f){Result=RpgBattleResult.Escaped;Message="Útěk se podařil.";return true;}
                Message="Útěk se nepodařil.";break;
            default:return false;
        }
        if(EnemyHealth==0){Result=RpgBattleResult.Victory;Message+=" Vítězství!";return true;}
        if(dodgeRoll<Player.DodgeChance)Message+=" Úhyb: nepřítel tě minul.";
        else
        {
            int damage=Mathf.Max(1,(Boss?22:9)-Player.Defense);
            if(guard && dodgeRoll<.35f)damage=0;
            else if(guard)damage=Mathf.Max(1,(damage+1)/2);
            Player.health=Mathf.Max(0,Player.health-damage);Message+=" Nepřítel útočí: −"+damage+" HP.";
        }
        if(Player.health<=0){Result=RpgBattleResult.Defeat;Message+=" Padl jsi.";}
        else Round++;
        return true;
    }
}

public sealed partial class RpgGame
{
    private RpgTurnBattle battle;
    private RpgEnemy battleEnemy;
    private bool finalBattle,autoBattle;
    private Coroutine autoBattleRoutine;
    public bool AutoBattle => autoBattle;
    public bool InBattle => battle!=null;
    public void TryStartNearbyBattle()
    {
        RpgEnemy nearest=null;float distance=2;
        foreach(var enemy in Enemies)
        {
            if(enemy==null || enemy.Dead)continue;
            float next=Vector2.Distance(transform.position,enemy.transform.position);
            if(next<distance){distance=next;nearest=enemy;}
        }
        if(nearest!=null)TryStartBattle(nearest);
        else Notify("Žádný nepřítel není v dosahu. Přijdi blíž.");
    }
    public void TryStartBattle(RpgEnemy enemy)
    {
        if(InBattle || BlockMovement || !HostileCombatAllowed || enemy==null || enemy.Dead)return;
        if(!IsTestLocation)State.RecordArchiveEnemy(enemy.IsFlying);
        Save(false);
        battleNpcId=null;battleEnemy=enemy;battle=new RpgTurnBattle(State,Mathf.CeilToInt(enemy.CurrentHealth),false,null,enemy.IsFlying);
        Interface.ShowBattle(battle);
    }
    public void ChooseBattleAction(RpgBattleAction action)
    {
        if(!InBattle || (battleEnemy==null && battleNpcId==null && !finalBattle))return;
        if(!battle.Act(action,Random.value,Random.value,Random.value)){Interface.ShowBattle(battle);return;}
        if(battle.Result==RpgBattleResult.Ongoing){Interface.ShowBattle(battle);return;}
        bool wasFinal=finalBattle;finalBattle=false;
        var result=battle.Result;var enemy=battleEnemy;string npc=battleNpcId;battle=null;battleEnemy=null;battleNpcId=null;
        Interface.Close();
        if(wasFinal)
        {
            if(result==RpgBattleResult.Victory){State.FinishFinalEnemy();Save(false);Interface.ShowFinalWave();}
            else {autoBattle=false;if(result==RpgBattleResult.Defeat)Interface.ShowDeath();else Interface.ShowFinalWave();}
            if(State.health>0)Save(false);
            return;
        }
        if(result==RpgBattleResult.Victory)
        {if(npc!=null)NpcVictory(npc);else enemy.Hit(Mathf.CeilToInt(enemy.CurrentHealth),Vector2.zero);}
        else if(result==RpgBattleResult.Defeat){if(enemy!=null)enemy.ResetHome();Interface.ShowDeath();}
        else {if(enemy!=null){enemy.ResetHome();enemy.WaitUntilPlayerLeaves();}Notify("Unikl jsi. Zranění a spotřebované lektvary zůstávají.");}
        if(State.health>0)Save(false);
    }
    public void StartFinalBattle()
    {
        if(InBattle || State.health<=0 || !State.finalRitualStarted || State.storyComplete || State.place!=RpgPlace.ChurchRuins)return;
        finalBattle=true;battleEnemy=null;battleNpcId=null;
        bool flying=State.finalEnemiesRemaining%2==0;
        battle=new RpgTurnBattle(State,32,false,RpgEnemy.KindName(flying),flying);
        State.RecordArchiveEnemy(flying);
        Interface.ShowBattle(battle);
    }
    public void ToggleAutoBattle()
    {
        if(autoBattle){StopAutoBattle();return;}
        if(!State.finalRitualStarted || State.storyComplete)return;
        autoBattle=true;autoBattleRoutine=StartCoroutine(RunAutoBattle());
    }
    private void StopAutoBattle()
    {
        autoBattle=false;if(autoBattleRoutine!=null){StopCoroutine(autoBattleRoutine);autoBattleRoutine=null;}
    }
    private IEnumerator RunAutoBattle()
    {
        while(autoBattle && State.health>0 && !State.storyComplete)
        {
            if(!InBattle)StartFinalBattle();
            if(!InBattle || !State.CanFight(battle.Flying)){autoBattle=false;Notify("Na autoboj potřebuješ výcvik a vhodné zbraně.");break;}
            var action=State.health<=35 && State.potions>0?RpgBattleAction.Potion:
                battle.Flying || !State.CanSwordFight?RpgBattleAction.Bow:RpgBattleAction.Attack;
            ChooseBattleAction(action);
            yield return new WaitForSecondsRealtime(.2f);
        }
        autoBattle=false;
    }

}

public sealed partial class RpgInterface
{
    public void ShowBattle(RpgTurnBattle battle)
    {
        Open("battle","TAHOVÝ SOUBOJ • "+battle.OpponentName.ToUpperInvariant(),
            "Kolo "+battle.Round+" • tvůj tah\n\nTvé zdraví: "+Mathf.CeilToInt(game.State.health)+" / "+game.State.MaxHealth+
            "\nNepřítel: "+battle.EnemyHealth+" HP\nLektvary: "+game.State.potions+"\n\n"+battle.Message+
            "\n\nPo tvé akci odpoví nepřítel. Čekání ani pohyb myši neposouvá tah.");
        modalTitle.resizeTextForBestFit=true;modalTitle.resizeTextMinSize=22;modalTitle.resizeTextMaxSize=28;
        if(game.State.CanFight(battle.Flying))
        {
            if(game.State.CanSwordFight && !battle.Flying)Action("Útok mečem",()=>game.ChooseBattleAction(RpgBattleAction.Attack));
            if(game.State.CanBowFight)Action("Výstřel z luku",()=>game.ChooseBattleAction(RpgBattleAction.Bow));
            Action("Blok • šance vykrýt zásah",()=>game.ChooseBattleAction(RpgBattleAction.Guard));
            Action("Lektvar • +15 HP",()=>game.ChooseBattleAction(RpgBattleAction.Potion));
        }
        Action("Pokusit se utéct • 50 %",()=>game.ChooseBattleAction(RpgBattleAction.Escape));
        if(game.State.finalRitualStarted)Action(game.AutoBattle?"Zastavit autoboj":"Spustit autoboj",game.ToggleAutoBattle);
    }
    public void ShowFinalWave()
    {
        var s=game.State;
        Open("finalWave",s.storyComplete?"ÚNIK ZE SNU":"OBRANA RITUÁLU",s.storyComplete?
            "Poslední stín padl. Hvězda se rozsvítí a cesta ven ze snu se otevírá. Dokončil jsi třetí cyklus.":
            $"Rituál přivolal nepřátele. Zbývá {s.finalEnemiesRemaining} z {RpgState.FinalWaveSize}. Můžeš bojovat tahově nebo spustit zrychlený autoboj. Spotřebuje skutečné lektvary a může skončit porážkou.");
        if(!s.storyComplete){Action("Další nepřítel",game.StartFinalBattle);Action(game.AutoBattle?"Zastavit autoboj":"Spustit autoboj",game.ToggleAutoBattle);}
        Action("Zavřít",Close);
    }
}
