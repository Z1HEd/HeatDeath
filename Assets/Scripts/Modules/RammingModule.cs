using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Ship))]
public class RammingModule : ModuleBase, IHitter
{
    [SerializeField] private ScalarStat damage = new ScalarStat(10f, 0f);
    [SerializeField] private ScalarStat knockbackPower = new ScalarStat(1f, 0f);

    public float Damage => damage;
    public float KnockbackPower => knockbackPower;

    public override void Recalculate()
    {
        damage.Recalculate(CurrentModifiers);
        knockbackPower.Recalculate(CurrentModifiers);
    }

    protected override void ResetValues()
    {
        damage.ResetToBase();
        knockbackPower.ResetToBase();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == null || collision.gameObject.layer == gameObject.layer)
            return;

        var hittable = collision.gameObject.GetComponent<IHittable>();
        if (hittable != null)
        {
            hittable.Hit(this);
            hittable.ApplyKnockback(this, collision);
        }
    }
}
