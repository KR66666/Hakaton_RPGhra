using UnityEngine;

public sealed class RpgEnemy : MonoBehaviour
{
    public string Id { get; private set; }
    public bool IsBoss => false; // Legacy API; only wolves and bats are spawned.
    public static string KindName(bool flying) => flying?"Netopýr":"Vlk";
    public bool IsFlying => Id!=null && Id.StartsWith("flying");
    public bool Dead => health <= 0;
    public float HealthFraction => health / maxHealth;
    private RpgGame game;
    private Vector2 home;
    private SpriteRenderer appearance;
    private TextMesh label;
    private float health, maxHealth;
    private bool waitingForSeparation;
    public float CurrentHealth => health;
    public void WaitUntilPlayerLeaves(){waitingForSeparation=true;}
    private Color tint;

    public void Initialize(RpgGame owner, string id, Vector2 origin, bool boss)
    {
        game = owner; Id = id; home = origin;
        maxHealth = health = 48 + game.State.currentArea * 10;
        appearance = gameObject.AddComponent<SpriteRenderer>();
        appearance.sprite = RpgVisualTheme.Icon(IsFlying?"bat":"wolf");
        tint = Color.white;
        if(IsFlying){appearance.sprite=RpgVisualTheme.Icon("bat");tint=new Color(.7f,.45f,.9f);}
        appearance.color = tint; appearance.sortingOrder = 3;
        transform.localScale = Vector3.one * .85f;
        label = RpgGame.AddLabel(transform, KindName(IsFlying), new Vector3(0, 0.9f, 0), new Color(1, 0.65f, 0.6f));
    }

    private void Update()
    {
        if(game==null || Dead)return;
        if(!game.IsTestLocation && Vector2.Distance(game.Player.position,transform.position)<3 && game.State.RecordArchiveEnemy(IsFlying))game.Save(false);
        label.text=KindName(IsFlying)+"\n"+Mathf.CeilToInt(health)+" / "+Mathf.CeilToInt(maxHealth);
        if(waitingForSeparation)
        {
            if(Vector2.Distance(game.Player.position,transform.position)>2.5f)waitingForSeparation=false;
            return;
        }
        if(game.BlockMovement || !game.HostileCombatAllowed)return;
        if(Vector2.Distance(game.Player.position,transform.position)<1.25f)game.TryStartBattle(this);
    }

    public void Hit(int amount, Vector2 direction)
    {
        if (Dead || !game.HostileCombatAllowed) return;
        health = Mathf.Max(0, health - amount);
        transform.position = game.MoveEnemy(transform.position, transform.position + (Vector3)(direction * (IsBoss ? 0.12f : 0.25f)));
        RpgGame.FloatingText(transform.position + Vector3.up * 0.6f, amount.ToString(), new Color(1, 0.85f, 0.35f));
        if (Dead)
        {
            appearance.enabled = false; label.gameObject.SetActive(false);
            game.EnemyDefeated(this);
            Destroy(gameObject, 0.1f);
        }
    }

    public void ResetHome()
    {
        if (Dead) return;
        transform.position = home; health = maxHealth;
    }
}
