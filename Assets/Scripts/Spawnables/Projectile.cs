using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField]
    public GameObject sparksPrefab;
    [Tooltip("Explosive shots only: left where the shot explodes, sized by its blast. Leave empty for none.")]
    [SerializeField] private HazardZone hazardPrefab;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("How much the spark direction leans toward the surface normal vs. continuing along the bullet's travel direction. 0 = pure travel direction, 1 = pure normal.")]
    private float sparkNormalBias = 0.3f;

    // What the shot carries, fixed when fired, where it was fired from, and the enemies it has hit (pierce).
    public HitPayload Payload { get; private set; }
    public Vector2 FiredFrom { get; private set; }
    private int enemiesHit;
    public List<Effect> appliesEffects = new();
    public List<Effect> GetEffects() { return appliesEffects; }
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    public bool isDead { get; protected set; }
    private int remainingPierceHits;
    private bool hasInfinitePierce;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Direction apart from speed, so a projectile with speed 0 (a laser) still knows where it points.
    public virtual void Initialize(Vector2 direction, float speed, ProjectileModule sourceModule)
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;

        Payload = sourceModule.CreatePayload();
        FiredFrom = transform.position;
        enemiesHit = 0;
        appliesEffects = sourceModule.GetHitEffects();
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
        if (hittable == null || hittable.IsDead)
            return false;
        // Pushes along the shot. A laser has no velocity, and no knockback either.
        DamageTaken taken = hittable.TakeHit(new Hit(Payload, FiredFrom, transform.position,
            rb.linearVelocity.normalized, enemiesHit, appliesEffects));
        if (hittable is Ship ship)
            Payload.ReportHit(ship.CoreModule, taken);
        enemiesHit++;

        SpawnSparks(collision);

        TryConsumeCharge();
        return true;
    }

    // After an explosion: leaves the shot's hazard zone, if it has one, over the blast.
    protected void LeaveHazard(Explosion explosion)
    {
        if (hazardPrefab != null)
            Instantiate(hazardPrefab, explosion.transform.position, Quaternion.identity)
                .Initialize(Payload, explosion.BlastRadius, gameObject.layer);
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

    protected virtual void SpawnSparks(Collider2D hitCollider)
    {
        if (sparksPrefab == null || hitCollider == null)
            return;

        Vector2 incomingDir = rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : (Vector2)transform.right;

        Vector2 normal = transform.position - hitCollider.transform.position;

        if (normal.sqrMagnitude < 0.0001f)
            normal = -incomingDir; // degenerate fallback, still outward-ish for head-on hits
        normal.Normalize();

        Vector2 sparkDir = Vector2.Lerp(incomingDir, normal, sparkNormalBias).normalized;
        if (sparkDir.sqrMagnitude < 0.0001f)
            sparkDir = incomingDir;

        float angle = Mathf.Atan2(sparkDir.y, sparkDir.x) * Mathf.Rad2Deg;

        GameObject sparks = Instantiate(sparksPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
        sparks.transform.right = sparkDir; // align prefab's local "forward" (right) axis with spark direction
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        TryApplyHit(collision);
    }
}