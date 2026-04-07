using UnityEngine;

[RequireComponent(typeof(Ship))]
public class RammingModule : ModuleBase, IHitter
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackPower = 1f;

    public float Damage { get => damage; set => damage = value; }
    public float KnockbackPower { get => knockbackPower; set => knockbackPower = value; }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == null)
            return;

        var hittable = collision.gameObject.GetComponent<IHittable>();
        if (hittable != null)
        {
            hittable.Hit(this);
            hittable.ApplyKnockback(this, collision);
        }
    }
}
