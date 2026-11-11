using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatModifierEffect : Effect
{
    [SerializeField] public StatType stat;
    [SerializeField] public StatModifier statModifier;
    [SerializeField] public List<TagDefinition> targetTags = new List<TagDefinition>();

    public bool HasTargetTags => targetTags != null && targetTags.Count > 0;
    public StatModifierEffect(){}
    public StatModifierEffect(StatType stat, StatModifier statModifier, List<TagDefinition> targetTags, ResourceStat duration)
    {
        this.stat = stat;
        this.statModifier = statModifier;
        this.targetTags = targetTags;
        this.duration = duration;
    }

    public override bool IsApplicableTo(ModuleDefinition moduleDefinition)
    {
        if (moduleDefinition == null) return false;
        return moduleDefinition.MatchesAnyTag(targetTags);
    }

    public override void ApplyToModule(ModuleBase module)
    {
        var map = module.currentModifiers;
        map.TryGetValue(stat, out StatModifier currentModifier);
        map[stat] = StatModifier.Combine(currentModifier, statModifier);
    }
    // Same stat, same targets, same direction: a slow and a speed-up never share a slot.
    public override bool SharesSlotWith(Effect other)
    {
        return other is StatModifierEffect o
            && o.stat == stat
            && Mathf.Sign(Direction) == Mathf.Sign(o.Direction)
            && HasSameTags(o.targetTags);
    }

    // Percent decides first, flat breaks ties.
    public override bool IsAtLeastAsStrongAs(Effect other)
    {
        if (other is not StatModifierEffect o)
            return true;

        float percent = Mathf.Abs(statModifier.Percent), otherPercent = Mathf.Abs(o.statModifier.Percent);
        if (percent != otherPercent)
            return percent > otherPercent;

        return Mathf.Abs(statModifier.Flat) >= Mathf.Abs(o.statModifier.Flat);
    }

    private float Direction => statModifier.Percent != 0f ? statModifier.Percent : statModifier.Flat;

    private bool HasSameTags(List<TagDefinition> otherTags)
    {
        if (targetTags.Count != otherTags.Count)
            return false;

        foreach (var tag in targetTags)
            if (!otherTags.Contains(tag))
                return false;

        return true;
    }

    protected override Effect Stacked(float times)
    {
        var copy = (StatModifierEffect)base.Stacked(times);
        copy.statModifier = statModifier * times;
        return copy;
    }
}
