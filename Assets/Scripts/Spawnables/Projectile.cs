using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private GameObject sparksPrefab;
    [Tooltip("Explosive shots only: left where the shot explodes, sized by its blast. Leave empty for none.")]
    [SerializeField] private HazardZone hazardPrefab;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("How much the spark direction leans toward the surface normal vs. continuing along the bullet's travel direction. 0 = pure travel direction, 1 = pure normal.")]
    private float sparkNormalBias = 0.3f;

    // What the shot carries, fixed when fired, where it was fired from, and the enemies it has hit (pierce).
    public HitPayload Payload { get; private set; }
    public Vector2 FiredFrom { get; private set; }
    private int enemiesHit;
    // Rolled again on every hit, so each ship it pierces and each blast it sets off rolls.
    protected HitEffects HitEffects { get; private set; }
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    public bool IsDead { get; protected set; }
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
        HitEffects = sourceModule.GetHitEffects();
        ConfigurePiercing(sourceModule.Piercing);
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

    // Stops and hides the shot, and removes it once its trail and effects have faded.
    public void Kill()
    {
        if (IsDead)
            return;

        IsDead = true;
        rb.linearVelocity = Vector3.zero;
        spriteRenderer.enabled = false;
        StartCoroutine(DestroyAfterFade());
    }

    private IEnumerator DestroyAfterFade()
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    // What the shot does to what it runs into: damage here, a blast for an explosive shot.
    protected virtual void Strike(IHittable hittable, Collider2D collision)
    {
        // Pushes along the shot. A laser has no velocity, and no knockback either.
        DamageTaken taken = hittable.TakeHit(new Hit(Payload, FiredFrom, transform.position,
            rb.linearVelocity.normalized, enemiesHit, HitEffects));
        if (hittable is Ship ship)
            Payload.ReportHit(ship.CoreModule, taken);
        enemiesHit++;

        SpawnSparks(collision);
    }

    // After an explosion: leaves the shot's hazard zone, if it has one, over the blast.
    protected void LeaveHazard(Explosion explosion)
    {
        if (hazardPrefab != null)
            Instantiate(hazardPrefab, explosion.transform.position, Quaternion.identity)
                .Initialize(Payload, explosion.BlastRadius, gameObject.layer);
    }

    // Each hit spends a pierce charge; the hit after the last one ends the shot.
    protected void ConsumeCharge()
    {
        if (IsDead || hasInfinitePierce)
            return;

        if (remainingPierceHits > 0)
            remainingPierceHits--;
        else
            Kill();
    }

    private void SpawnSparks(Collider2D hitCollider)
    {
        if (sparksPrefab == null)
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
        if (IsDead)
            return;

        var hittable = collision.GetComponent<IHittable>();
        if (hittable == null || hittable.IsDead)
            return;

        Strike(hittable, collision);
        ConsumeCharge();
    }
}