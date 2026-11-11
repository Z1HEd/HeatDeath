using System;
using UnityEngine;

// Applied to the ship once when added, and to its modules and appearance on every recalculation.
// Override only the hooks an effect needs.
[Serializable]
public abstract class Effect
{
    [SerializeField]
    protected ResourceStat duration = new ResourceStat(StatType.Duration,float.PositiveInfinity,0f);
    [NonSerialized] private float elapsed;
    // The asset effect this is a copy of, so EffectManager.Apply can match copies of the same effect.
    [NonSerialized] private Effect template;

    public float Duration => duration.BaseValue;
    public Effect Template => template ?? this;

    public virtual bool Update(float delta)
    {
        elapsed += delta;
        return elapsed < duration.BaseValue;
    }

    // The only way to hand out an effect: definition assets are shared and must not change at runtime.
    // The copy remembers which asset effect it comes from.
    public Effect Copy(float times = 1f)
    {
        Effect copy = Stacked(times);
        copy.elapsed = 0f;
        copy.template = Template;
        return copy;
    }

    // Restarts the duration without reapplying the effect.
    public void Restart() => elapsed = 0f;

    // A copy at times the strength, for stacks and multipliers. The default clones the effect, which fits
    // effects with nothing to scale. Override it to scale strength, and IsAtLeastAsStrongAs with it.
    protected virtual Effect Stacked(float times)
    {
        var copy = (Effect)MemberwiseClone();
        copy.duration = new ResourceStat(duration.Type, duration.BaseValue, duration.MinValue);
        return copy;
    }

    public virtual bool IsApplicableTo(ModuleDefinition definition) => false;
    public virtual void ApplyToModule(ModuleBase module) {}
    public virtual void ApplyToShip(Ship ship) {}
    public virtual void RemoveFromShip(Ship ship) {}

    // Looks and ghosting: rebuilt from every active effect on each recalculation, like stats.
    public virtual void ApplyToAppearance(ShipAppearance appearance) {}

    // On-hit effects in the same slot don't stack: see EffectManager.AddStatusEffect.
    public virtual bool SharesSlotWith(Effect other) => other != null && other.GetType() == GetType();

    // Compared between effects that share a slot, and between copies of one effect in EffectManager.Apply.
    // Equal strength counts as true, so a repeat hit restarts.
    public virtual bool IsAtLeastAsStrongAs(Effect other) => true;

    // Only for fresh copies. Permanent effects stay permanent.
    public void ModifyDuration(StatModifier modifier)
    {
        if (float.IsPositiveInfinity(duration.BaseValue))
            return;

        float modified = Mathf.Max(0f, (duration.BaseValue + modifier.Flat) * (1f + modifier.Percent / 100f));
        duration = new ResourceStat(duration.Type, modified, duration.MinValue);
    }
}
