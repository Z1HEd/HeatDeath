using System.Collections;
using UnityEngine;

public class LaserProjectile : Projectile
{
    [SerializeField] private float lifeTime = 0.12f;
    private float lifeTimeLeft = 0f;
    private bool isFired = false;
    [SerializeField] private Color baseColor = new Color(1f,1f,1f);
    public override void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        base.Initialize(Vector2.zero, sourceModule);
        transform.up = velocity.normalized;
        lifeTimeLeft = lifeTime;
        isFired = true;
    }

    public override IEnumerator Kill()
    {
        yield break;
    }
    public void Update()
    {
        if (isDead || !isFired) return;

        lifeTimeLeft -= Time.deltaTime;
        if (lifeTimeLeft < 0)
        {
            isDead = true;
            Destroy(gameObject);
        }
        
        spriteRenderer.color = new Color(baseColor.r,baseColor.g,baseColor.b,lifeTimeLeft/lifeTime);
    }
}
