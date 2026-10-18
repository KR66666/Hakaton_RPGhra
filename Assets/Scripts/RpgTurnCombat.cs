using UnityEngine;

public enum RpgBattleAction { Attack, Guard, Potion, Escape }
public enum RpgBattleResult { Ongoing, Victory, Defeat, Escaped }

// A round advances only when the player chooses a valid action.
public sealed class RpgTurnBattle
{
    public readonly RpgState Player;
    public readonly bool Boss;
    public readonly string OpponentName;
    public int EnemyHealth { get; private set; }
    public int Round { get; private set; } = 1;
    public RpgBattleResult Result { get; private set; }
    public string Message { get; private set; } = "Jsi na tahu. Vyber akci.";
    public RpgTurnBattle(RpgState player,int enemyHealth,bool boss,string name=null)
    {Player=player;EnemyHealth=enemyHealth;Boss=boss;OpponentName=name??(boss?"Velitel":"Nájezdník");}
    public bool Act(RpgBattleAction action,float instantRoll,float dodgeRoll,float escapeRoll)
    {
        if(Result!=RpgBattleResult.Ongoing)return false;
        bool guard=false;
        switch(action)
        {
            case RpgBattleAction.Attack:
                bool instant=!Boss && instantRoll<Player.InstantWinChance;
                int hit=instant?EnemyHealth:Player.Damage;
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
            if(guard)damage=Mathf.Max(1,(damage+1)/2);
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
        Save(false);
        battleNpcId=null;battleEnemy=enemy;battle=new RpgTurnBattle(State,Mathf.CeilToInt(enemy.CurrentHealth),enemy.IsBoss);
        Interface.ShowBattle(battle);
    }
    public void ChooseBattleAction(RpgBattleAction action)
    {
        if(!InBattle || (battleEnemy==null && battleNpcId==null))return;
        if(!battle.Act(action,Random.value,Random.value,Random.value)){Interface.ShowBattle(battle);return;}
        if(battle.Result==RpgBattleResult.Ongoing){Interface.ShowBattle(battle);return;}
        var result=battle.Result;var enemy=battleEnemy;string npc=battleNpcId;battle=null;battleEnemy=null;battleNpcId=null;
        Interface.Close();
        if(result==RpgBattleResult.Victory)
        {if(npc!=null)NpcVictory(npc);else enemy.Hit(Mathf.CeilToInt(enemy.CurrentHealth),Vector2.zero);}
        else if(result==RpgBattleResult.Defeat){if(enemy!=null)enemy.ResetHome();Interface.ShowDeath();}
        else {if(enemy!=null){enemy.ResetHome();enemy.WaitUntilPlayerLeaves();}Notify("Unikl jsi. Zranění a spotřebované lektvary zůstávají.");}
        if(State.health>0)Save(false);
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
        Action("Útok",()=>game.ChooseBattleAction(RpgBattleAction.Attack));
        Action("Krýt se • poloviční zásah",()=>game.ChooseBattleAction(RpgBattleAction.Guard));
        Action("Lektvar • +60 HP",()=>game.ChooseBattleAction(RpgBattleAction.Potion));
        actions.GetChild(2).GetComponent<UnityEngine.UI.Button>().interactable=game.State.potions>0 && game.State.health<game.State.MaxHealth;
        Action("Pokusit se utéct • 50 %",()=>game.ChooseBattleAction(RpgBattleAction.Escape));
    }
}
