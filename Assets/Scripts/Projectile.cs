using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IHitter
{
    private float damage = 10f;
    private float knockbackPower = 5f;
    private Rigidbody2D rb;

    public float Damage { get => damage; set => damage = value; }
    public float KnockbackPower { get => knockbackPower; set => knockbackPower = value; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }

    public void Initialize(Vector2 velocity, float dmg, float knockback)
    {
        if (rb != null)
            rb.linearVelocity = velocity;
        damage = dmg;
        knockbackPower = knockback;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == null)
            return;

        var hittable = collision.GetComponent<IHittable>();
        if (hittable == null) return;

        hittable.Hit(this);
        if (collision.GetComponent<Rigidbody2D>() != null)
        {
            // Store collision for knockback that might be applied
            hittable.ApplyKnockback(this, null);
        }
        Destroy(gameObject);

    }
}
