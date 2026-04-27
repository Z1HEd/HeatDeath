using System.Collections;
using UnityEngine;

public class LaserProjectile : Projectile
{
    [SerializeField] private float lifeTime = 0.12f;

    public override void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        base.Initialize(Vector2.zero, sourceModule);

        transform.up = velocity.normalized;
        StartCoroutine(DestroyAfterLifetime());
    }

    public override void Kill()
    {
        // Laser ignores outside-screen cleanup and dies by lifetime.
    }

    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, lifeTime));

        if (!isDead)
            Destroy(gameObject);
    }
}
