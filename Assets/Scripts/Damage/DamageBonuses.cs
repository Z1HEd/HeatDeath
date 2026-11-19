using UnityEngine;

// A weapon's damage bonuses that depend on the hit, all in percent, from its stats. Projectiles copy them when
// fired, so a shot keeps its bonuses after its weapon is gone. The bonus factors multiply each other and the
// whole hit, percent max hull and percent shields included.
public struct DamageBonuses
{
    public float PercentMaxHull;  // Of the target's max hull, added to the hit's damage.
    public float PercentShields;  // Of the target's current shields, added to the hit's damage.
    public float PerUnit;         // Per unit between where the shot was fired and the target. Negative: falloff.
    public float PerEnemyHit;     // Per enemy the shot hit before this one: pierced, blasted, or chained.
    public int MaxEnemiesCounted; // The most enemies PerEnemyHit counts.
    public float Execute;         // At 0 hull, less the more hull the target has left.
    public float VsDebuffed;      // Against targets carrying any on-hit debuff.
    public float PerFocusStack;   // Per hit in a row on the payload's focus target.

    // A hit's damage against this target, before its shield and hull multipliers. A hit without bonuses (ramming,
    // abilities) deals its payload's damage.
    public float Apply(in Hit hit, ShipCoreModule target)
    {
        float distance = Vector2.Distance(hit.FiredFrom, target.transform.position);
        float factor = hit.DamageScale
                     * Mathf.Max(0f, 1f + PerUnit / 100f * distance)
                     * (1f + PerEnemyHit / 100f * Mathf.Min(hit.EnemiesHitBefore, MaxEnemiesCounted))
                     * (1f + Execute / 100f * (1f - target.HealthFraction))
                     * (target.IsDebuffed ? 1f + VsDebuffed / 100f : 1f)
                     * (target == hit.Payload.FocusTarget ? 1f + PerFocusStack / 100f * hit.Payload.FocusStacks : 1f);
        return (hit.Payload.Damage + PercentMaxHull / 100f * target.MaxHealth
                + PercentShields / 100f * target.ActiveShields) * factor;
    }
}
