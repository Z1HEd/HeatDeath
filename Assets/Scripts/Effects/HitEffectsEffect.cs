using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Gives the modules it targets extra on-hit effects: shots that slow, stun, jam, or expose. For upgrades.
[Serializable]
public class HitEffectsEffect : Effect
{
    [SerializeField] private List<TagDefinition> targetTags = new List<TagDefinition>();
    [Tooltip("Chance per hit that the effects apply: each ship a shot hits, and each blast, rolls on its own. Stacks " +
             "multiply it, up to 1.")]
    [SerializeField, Range(0f, 1f)] private float chance = 1f;
    [Tooltip("Applied to the ship that's hit.")]
    [SerializeReference, SubclassSelector] private List<Effect> effects = new List<Effect>();
    [Tooltip("Stacks also multiply the effects' strength. Off: a debuff keeps the strength it's sized for, and stacks " +
             "only raise its chance.")]
    [SerializeField] private bool stacksRaiseStrength;
    [Tooltip("Stacks also multiply the effects' durations: for stuns, which are always at full strength.")]
    [SerializeField] private bool stacksRaiseDuration;

    public float Chance => chance;
    public IReadOnlyList<Effect> Effects => effects;

    public override bool IsApplicableTo(ModuleDefinition definition) =>
        definition != null && definition.MatchesAnyTag(targetTags);

    public override void ApplyToModule(ModuleBase module) => module.GrantHitEffects(this);

    // Chance first, then each effect's duration and strength, so EffectManager.SetEffects replaces the copy when
    // stacks change.
    public override bool IsAtLeastAsStrongAs(Effect other)
    {
        if (other is not HitEffectsEffect o || o.effects.Count != effects.Count)
            return true;
        if (chance != o.chance)
            return chance > o.chance;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null || o.effects[i] == null)
                continue;
            if (effects[i].Duration != o.effects[i].Duration)
                return effects[i].Duration > o.effects[i].Duration;
            if (!effects[i].IsAtLeastAsStrongAs(o.effects[i]))
                return false;
        }
        return true;
    }

    protected override Effect Stacked(float times)
    {
        var copy = (HitEffectsEffect)base.Stacked(times);
        copy.chance = Mathf.Min(1f, chance * times);
        float strength = stacksRaiseStrength ? times : 1f;
        copy.effects = effects.Where(e => e != null).Select(e =>
        {
            Effect stacked = e.Copy(strength);
            if (stacksRaiseDuration)
                stacked.ModifyDuration(new StatModifier(0f, (times - 1f) * 100f));
            return stacked;
        }).ToList();
        return copy;
    }
}
