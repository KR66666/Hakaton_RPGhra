using UnityEngine;

public sealed class RpgEnemy : MonoBehaviour
{
    public string Id { get; private set; }
    public bool IsBoss { get; private set; }
    public bool Dead => health <= 0;
    public float HealthFraction => health / maxHealth;
    private RpgGame game;
    private Vector2 home;
    private SpriteRenderer appearance;
    private TextMesh label;
    private float health, maxHealth, nextStrike, hitUntil, telegraphUntil;
    private bool preparing;
    private Color tint;

    public void Initialize(RpgGame owner, string id, Vector2 origin, bool boss)
    {
        game = owner; Id = id; home = origin; IsBoss = boss;
        maxHealth = health = (boss ? 220 : 48) + game.State.currentArea * (boss ? 35 : 10);
        appearance = gameObject.AddComponent<SpriteRenderer>();
        appearance.sprite = MedievalNpcAppearance.Get(boss ? "boss" : "bandit", id.Length > 0 ? id[id.Length-1] % 4 : 0);
        tint = Color.white;
        appearance.color = tint; appearance.sortingOrder = 3;
        transform.localScale = Vector3.one * (boss ? 1.25f : 0.85f);
        label = RpgGame.AddLabel(transform, boss ? "VELITEL VRANEK" : "Nájezdník", new Vector3(0, 0.9f, 0), new Color(1, 0.65f, 0.6f));
    }

    private void Update()
    {
        if (game == null || Dead || game.BlockMovement) return;
        appearance.color = Time.time < hitUntil ? Color.white : preparing ? new Color(1, 0.25f, 0.15f) : tint;
        label.text = (IsBoss ? "VELITEL VRANEK" : "Nájezdník") + "\n" + Mathf.CeilToInt(health) + " / " + Mathf.CeilToInt(maxHealth);
        if (game.State.questStage == 0) return;
        Vector2 delta = game.Player.position - transform.position;
        float distance = delta.magnitude;
        if (preparing)
        {
            if (Time.time >= telegraphUntil)
            {
                preparing = false;
                if (distance < (IsBoss ? 1.65f : 1.15f)) game.Damage(IsBoss ? 22 : 9);
                nextStrike = Time.time + (IsBoss ? 1.5f : 1.8f);
            }
            return;
        }
        if (distance < (IsBoss ? 1.25f : 0.85f) && Time.time >= nextStrike)
        {
            preparing = true; telegraphUntil = Time.time + (IsBoss ? 0.65f : 0.45f);
        }
        else if (distance < (IsBoss ? 6 : 3.6f) && distance > 0.65f && Time.time >= hitUntil)
        {
            // Keep bodies apart so a crowd cannot occupy a single point.
            var direction = delta.normalized;
            foreach (var other in game.Enemies)
            {
                if (other == null || other == this || other.Dead) continue;
                Vector2 away = transform.position - other.transform.position;
                if (away.sqrMagnitude > 0.001f && away.sqrMagnitude < 0.5f)
                    direction += away.normalized * 0.65f;
            }
            var desired = transform.position + (Vector3)(direction.normalized * (IsBoss ? 1.25f : 1.05f) * Time.deltaTime);
            transform.position = game.MoveEnemy(transform.position, desired);
        }
        else if (distance >= 5 && Vector2.Distance(transform.position, home) > 0.1f)
        {
            transform.position = game.MoveEnemy(transform.position, Vector3.MoveTowards(transform.position, home, Time.deltaTime));
        }
    }

    public void Hit(int amount, Vector2 direction)
    {
        if (Dead || game.State.questStage == 0) return;
        health = Mathf.Max(0, health - amount);
        hitUntil = Time.time + 0.16f;
        preparing = false;
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
        transform.position = home; health = maxHealth; preparing = false; nextStrike = Time.time + 3;
    }
}
