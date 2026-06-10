using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IHitter
{
    public float Damage { get; protected set; } = 10f;
    public float ShieldDamageMultiplier { get; protected set; } = 1f;
    public float HPDamageMultiplier { get; protected set; } = 1f;
    public float KnockbackPower { get; protected set; } = 0f;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    public bool isDead {get;protected set;}
    private int remainingPierceHits;
    private bool hasInfinitePierce;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public virtual void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        if (rb != null)
            rb.linearVelocity = velocity;

        Damage = sourceModule.projectileDamage;
        KnockbackPower = sourceModule.projectileKnockback;
        ShieldDamageMultiplier = sourceModule.ShieldDamageMultiplier;
        HPDamageMultiplier = sourceModule.HPDamageMultiplier;
        ConfigurePiercing(sourceModule.projectilePiercing);
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

    public virtual IEnumerator Kill()
    {
        if (isDead)
            yield break;

        isDead = true;
        rb.linearVelocity = Vector3.zero;
        spriteRenderer.enabled = false;

        yield return new WaitForSeconds(0.2f); // Let trails and effects clear themselfes
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
            hittable.ApplyKnockback(this, transform.position-collision.transform.position);

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

        StartCoroutine(Kill());
        return true;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        TryApplyHit(collision);
    }
}

