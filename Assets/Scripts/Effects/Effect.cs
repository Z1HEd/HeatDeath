using System;
using System.Collections.Generic;
using UnityEngine;

// The effect is applied once to the ship when added
// It is however applied to a module each module recalculation
[Serializable]
public abstract class Effect
{
    [SerializeField]
    protected ResourceStat duration = new ResourceStat(StatType.Duration,float.PositiveInfinity,0f);
    [NonSerialized] private float elapsed;
    public virtual bool Update(float delta)
    {
        elapsed += delta;
        return elapsed < duration.BaseValue;
    }
    public abstract bool IsApplicableTo(ModuleDefinition definition);
    public abstract void ApplyToModule(ModuleBase module);
    public abstract void ApplyToShip(Ship ship);
    public abstract void RemoveFromShip(Ship ship);
    public abstract Effect Stacked(float times);

    // On-hit effects in the same slot don't stack: see EffectManager.AddStatusEffect.
    public virtual bool SharesSlotWith(Effect other) => other != null && other.GetType() == GetType();

    // Only compared between effects that share a slot. Equal strength counts as true, so a repeat hit restarts.
    public virtual bool IsAtLeastAsStrongAs(Effect other) => true;

    // Only for fresh copies: effects from definition assets are shared and must not change.
    // Permanent effects stay permanent.
    public void ModifyDuration(StatModifier modifier)
    {
        if (float.IsPositiveInfinity(duration.BaseValue))
            return;

        float modified = Mathf.Max(0f, (duration.BaseValue + modifier.Flat) * (1f + modifier.Percent / 100f));
        duration = new ResourceStat(duration.Type, modified, duration.MinValue);
    }
}