using UnityEngine;

public class Explosion : MonoBehaviour, IHitter
{
    public float Damage { get; private set; }
    public float KnockbackPower { get; private set; }
    public float timer = -1;
    public const float FADE_DURATION = 0.3f;
    private SpriteRenderer spriteRenderer;

    public void Initialize(float damage, float knockbackPower, float radius, int detectLayer)
    {
        Damage = damage;
        KnockbackPower = knockbackPower;
        spriteRenderer = GetComponent<SpriteRenderer>();

        transform.localScale = new Vector3(radius,radius,radius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, 1<<(detectLayer-2));
        foreach (Collider2D col in hits)
        {
            Ship ship = col.GetComponent<Ship>();
            if (ship == null || ship.IsDead)
                continue;
            
            ship.Hit(this);
            if (KnockbackPower > 0f)
                ship.ApplyKnockback(this, null);
        }
        timer = FADE_DURATION;
        
    }
    public void Update()
    {
        if (timer<0) return;
        timer -= Time.deltaTime;

        spriteRenderer.color = new Color(1f,1f,1f,timer/FADE_DURATION);

        if (timer<0)
            Destroy(gameObject);
    }
}
