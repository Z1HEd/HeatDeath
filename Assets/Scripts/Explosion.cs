using UnityEngine;

public class Explosion : MonoBehaviour, IHitter
{
    public float Damage { get; private set; }
    public float KnockbackPower { get; private set; }

    public void Initialize(float damage, float knockbackPower, float radius, int detectLayer)
    {
        Damage = damage;
        KnockbackPower = knockbackPower;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, 1 << detectLayer);
        foreach (Collider2D col in hits)
        {
            Ship ship = col.GetComponent<Ship>();
            if (ship == null || ship.IsDead)
                continue;

            ship.Hit(this);
            if (KnockbackPower > 0f)
                ship.ApplyKnockback(this, null);
        }

        Destroy(gameObject);
    }
}
