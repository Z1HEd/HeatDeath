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
    public override void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        base.Initialize(velocity,sourceModule);
        if (sourceModule is MineModule) 
            explosionRange = (sourceModule as MineModule).ExplosionRange;
        else
            Debug.LogError("Mine expects MineModule, got: "+ sourceModule);
    }

    public void ForceExplode()
    {
        Explode();
        Kill();
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
