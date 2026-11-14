using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Gives the modules it targets extra on-hit effects: shots that slow, stun, jam, or expose. For upgrades.
[Serializable]
public class HitEffectsEffect : Effect
{
    [SerializeField] public List<TagDefinition> targetTags = new List<TagDefinition>();
    [Tooltip("Chance per shot (per target for Tesla) that the effects apply. Stacks multiply it, up to 1.")]
    [SerializeField, Range(0f, 1f)] private float chance = 1f;
    [Tooltip("Applied to the ship that's hit. Stacks multiply their strength.")]
    [SerializeReference, SubclassSelector] private List<Effect> effects = new List<Effect>();

    public float Chance => chance;
    public IReadOnlyList<Effect> Effects => effects;

    public override bool IsApplicableTo(ModuleDefinition definition) =>
        definition != null && definition.MatchesAnyTag(targetTags);

    public override void ApplyToModule(ModuleBase module) => module.GrantHitEffects(this);

    // Chance first, then each effect, so EffectManager.Apply replaces the copy when stacks change.
    public override bool IsAtLeastAsStrongAs(Effect other)
    {
        if (other is not HitEffectsEffect o || o.effects.Count != effects.Count)
            return true;
        if (chance != o.chance)
            return chance > o.chance;

        for (int i = 0; i < effects.Count; i++)
            if (effects[i] != null && o.effects[i] != null && !effects[i].IsAtLeastAsStrongAs(o.effects[i]))
                return false;
        return true;
    }

    protected override Effect Stacked(float times)
    {
        var copy = (HitEffectsEffect)base.Stacked(times);
        copy.chance = Mathf.Min(1f, chance * times);
        copy.effects = effects.Where(e => e != null).Select(e => e.Copy(times)).ToList();
        return copy;
    }
}
