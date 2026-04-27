using UnityEngine;

public class MineProjectile : Projectile
{
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float drag = 8f;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null)
            rb.linearDamping = drag;
    }

    public void ForceExplode()
    {
        Explode();
        Kill();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Ship>() == null) return;
        Explode();
    }

    private void Explode()
    {
        if (isDead) return;

        if (explosionPrefab != null)
        {
            Explosion exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            exp.Initialize(Damage, KnockbackPower, explosionRadius, gameObject.layer);
        }

        TryConsumeCharge();
    }
}
