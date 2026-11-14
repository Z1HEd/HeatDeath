using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatModifierEffect : Effect
{
    [SerializeField] public StatType stat;
    [SerializeField] public StatModifier statModifier;
    [Tooltip("Added once per player level on top of statModifier: weak early, strong late.")]
    [SerializeField] public StatModifier perLevel;
    [SerializeField] public List<TagDefinition> targetTags = new List<TagDefinition>();

    public bool HasTargetTags => targetTags != null && targetTags.Count > 0;

    // The modifier at the player's current level. The player's effects are recalculated on each level-up.
    public StatModifier CurrentModifier
    {
        get
        {
            int level = GameController.Instance != null ? GameController.Instance.CurrentLevel : 0;
            return new StatModifier(statModifier.Flat + perLevel.Flat * level,
                                    statModifier.Percent + perLevel.Percent * level);
        }
    }
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
        map[stat] = StatModifier.Combine(currentModifier, CurrentModifier);
    }
    // Same stat, same targets, same direction: a slow and a speed-up never share a slot.
    public override bool SharesSlotWith(Effect other)
    {
        return other is StatModifierEffect o
            && o.stat == stat
            && Mathf.Sign(Direction) == Mathf.Sign(o.Direction)
            && HasSameTags(o.targetTags);
    }

    // Percent decides first, flat breaks ties, then the same for the per-level part.
    public override bool IsAtLeastAsStrongAs(Effect other)
    {
        if (other is not StatModifierEffect o)
            return true;

        float[] mine = { statModifier.Percent, statModifier.Flat, perLevel.Percent, perLevel.Flat };
        float[] theirs = { o.statModifier.Percent, o.statModifier.Flat, o.perLevel.Percent, o.perLevel.Flat };
        for (int i = 0; i < mine.Length; i++)
            if (Mathf.Abs(mine[i]) != Mathf.Abs(theirs[i]))
                return Mathf.Abs(mine[i]) > Mathf.Abs(theirs[i]);

        return true;
    }

    private float Direction
    {
        get
        {
            StatModifier current = CurrentModifier;
            return current.Percent != 0f ? current.Percent : current.Flat;
        }
    }

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
        copy.perLevel = perLevel * times;
        return copy;
    }
}
