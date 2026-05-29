using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IHitter
{
    public float Damage { get; protected set; } = 10f;
    public float KnockbackPower { get; protected set; } = 0f;
    protected Rigidbody2D rb;
    protected bool isDead;
    private int remainingPierceHits;
    private bool hasInfinitePierce;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        if (rb != null)
            rb.linearVelocity = velocity;

        Damage = sourceModule.ProjectileDamage;
        KnockbackPower = sourceModule.ProjectileKnockback;
        ConfigurePiercing(sourceModule.ProjectilePiercing);
    }

    private void ConfigurePiercing(float piercing)
    {
        if (piercing < 0f)
        {
            hasInfinitePierce = true;
            remainingPierceHits = 0;
            return;
        }

        hasInfinitePierce = false;
        remainingPierceHits = Mathf.Max(0, Mathf.FloorToInt(piercing));
    }

    public virtual void Kill()
    {
        if (isDead)
            return;

        isDead = true;
        Destroy(gameObject);
    }

    protected virtual bool TryApplyHit(Collider2D collision)
    {
        if (isDead)
            return false;

        if (collision.gameObject == null)
            return false;

        var hittable = collision.GetComponent<IHittable>();
        if (hittable == null)
            return false;

        hittable.Hit(this);
        if (collision.GetComponent<Rigidbody2D>() != null)
            hittable.ApplyKnockback(this, null);

        TryConsumeCharge();
        return true;
    }

    // Returns true if a charge was available. Kills self when last charge is consumed.
    protected bool TryConsumeCharge()
    {
        if (isDead) return false;
        if (hasInfinitePierce) return true;

        if (remainingPierceHits > 0)
        {
            remainingPierceHits--;
            return true;
        }

        Kill();
        return true;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        TryApplyHit(collision);
    }
}

