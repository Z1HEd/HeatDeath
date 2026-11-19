using UnityEngine;

public class MineProjectile : Projectile
{
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField] private float explosionRange = 3f;
    [SerializeField] private float drag = 8f;
    [Tooltip("Seconds a mine that survives its explosion ignores enemies before it can go off again.")]
    [SerializeField] private float rearmTime;
    private float armedAt;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null)
            rb.linearDamping = drag;
    }
    public override void Initialize(Vector2 direction, float speed, ProjectileModule sourceModule)
    {
        base.Initialize(direction, speed, sourceModule);
        if (sourceModule is MineModule) 
            explosionRange = (sourceModule as MineModule).ExplosionRange;
        else
            Debug.LogError("Mine expects MineModule, got: "+ sourceModule);
    }

    // Explodes and always removes the mine, even one that survives its explosions (infinite piercing).
    public void ForceExplode()
    {
        Explode();
        if (!isDead)
            StartCoroutine(Kill());
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Ship>() == null  || collision.GetComponent<Ship>().IsDead) return;
        if (Time.time < armedAt) return;
        Explode();
    }

    private void Explode()
    {
        if (isDead) return;
        armedAt = Time.time + rearmTime;

        if (explosionPrefab != null)
        {
            Explosion exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            exp.Initialize(Payload, FiredFrom, GetEffects(), explosionRange, gameObject.layer);
            LeaveHazard(exp);
        }

        TryConsumeCharge();
    }
}
