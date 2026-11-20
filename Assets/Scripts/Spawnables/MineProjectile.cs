using UnityEngine;

public class MineProjectile : Projectile
{
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField] private float drag = 8f;
    [Tooltip("Seconds a mine that survives its explosion ignores enemies before it can go off again.")]
    [SerializeField] private float rearmTime;
    // From the launcher, as thrown.
    private float blastDiameter;
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
        if (sourceModule is MineModule launcher)
            blastDiameter = launcher.BlastDiameter;
        else
            Debug.LogError("Mine expects MineModule, got: " + sourceModule);
    }

    // Explodes and always removes the mine, even one that survives its explosions (infinite piercing).
    public void ForceExplode()
    {
        Explode();
        Kill();
    }

    // Proximity fuse: a living ship that comes close sets it off, once it's armed.
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        var ship = collision.GetComponent<Ship>();
        if (ship != null && !ship.IsDead && Time.time >= armedAt)
            Explode();
    }

    private void Explode()
    {
        if (IsDead) return;
        armedAt = Time.time + rearmTime;

        if (explosionPrefab != null)
        {
            Explosion exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            exp.Initialize(Payload, FiredFrom, HitEffects, blastDiameter, gameObject.layer);
            LeaveHazard(exp);
        }

        ConsumeCharge();
    }
}
