using System;
using UnityEngine;

// Applied to the ship once when added, and to its modules and appearance on every recalculation.
// Override only the hooks an effect needs.
[Serializable]
public abstract class Effect
{
    [SerializeField]
    protected ResourceStat duration = new ResourceStat(StatType.Duration,float.PositiveInfinity,0f);
    [Tooltip("Shown on the ship while this effect applies (a stun's sparks, a slow's smoke). Effects with the same " +
             "prefab share one visual. For a debuff made of several effects, set it on the part that defines it: a " +
             "stun's Thrust, since a full jam can match its weapon part and keep it waiting.")]
    [SerializeField] private StatusVisual visual;
    // Time since it started, so an effect that wasn't copied (one saved in a prefab, or built in code) starts at its
    // full duration.
    [NonSerialized] private float elapsed;
    // The asset effect this is a copy of, so EffectManager can match copies of the same effect.
    [NonSerialized] private Effect template;

    public float Duration => duration.BaseValue;
    public float TimeLeft => Duration - elapsed;
    public StatusVisual Visual => visual;
    public Effect Template => template ?? this;

    // Runs while the effect applies. Returns false once it has run out.
    public virtual bool Update(float delta) => Elapse(delta);

    // Only the time passes: for an on-hit effect waiting behind a stronger one in its slot.
    public bool Elapse(float delta)
    {
        elapsed += delta;
        return elapsed < Duration;
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

    // A repeat of this effect lasts as long as the longer of the two, and changes nothing else (a DOT keeps its ticks).
    // A repeat that lasts longer than this copy's whole duration moves its start ahead of now.
    public void ExtendTo(float time)
    {
        if (time > TimeLeft)
            elapsed = Duration - time;
    }

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

    // On-hit effects in the same slot don't stack: the strongest applies. See EffectManager.AddHitEffect.
    public virtual bool SharesSlotWith(Effect other) => other != null && other.GetType() == GetType();

    // Compared between effects that share a slot, and between copies of one effect in EffectManager. Equal strength
    // counts as true, so a repeat hit extends the effect.
    public virtual bool IsAtLeastAsStrongAs(Effect other) => true;

    // Called on each copy a module hands out (ModuleBase.EmitEffects), so an effect that deals damage can credit it.
    public virtual void OnEmittedBy(ModuleBase module) {}

    // Only for fresh copies. Permanent effects stay permanent.
    public void ModifyDuration(StatModifier modifier)
    {
        if (float.IsPositiveInfinity(duration.BaseValue))
            return;

        float modified = Mathf.Max(0f, (duration.BaseValue + modifier.Flat) * (1f + modifier.Percent / 100f));
        duration = new ResourceStat(duration.Type, modified, duration.MinValue);
    }
}
