using UnityEngine;

public class MineProjectile : Projectile
{
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField] private float explosionRange = 3f;
    [SerializeField] private float drag = 8f;

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
        Explode();
    }

    private void Explode()
    {
        if (isDead) return;

        if (explosionPrefab != null)
        {
            Explosion exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            exp.Initialize(this, explosionRange, gameObject.layer);
        }

        TryConsumeCharge();
    }
}
