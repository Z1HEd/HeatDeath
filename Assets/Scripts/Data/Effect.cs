using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct StatModifier
{
    public float Flat;
    public float Percent;
    public StatModifier(float flat, float percent)
    {
        Flat = flat;
        Percent = percent;
    }

    public static StatModifier operator*(StatModifier left, float right) 
    {
        return new StatModifier(left.Flat * right, left.Percent * right);
    }
}

[Serializable]
public struct Effect
{
    [SerializeField] public StatType stat;
    [SerializeField] public StatModifier statModifier;
    [SerializeField] public List<TagDefinition> targetTags;

    public bool HasTargetTags => targetTags != null && targetTags.Count > 0;
    public Effect(StatType Stat, StatModifier Modifier, List<TagDefinition> TargetTags)
    {
        stat = Stat;
        statModifier = Modifier;
        targetTags = TargetTags;
    }

    public bool IsApplicableTo(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.MatchesAnyTag(targetTags);
    }
    public void ApplyToMap(Dictionary<StatType, StatModifier> map)
    {
        if (!map.TryGetValue(stat, out StatModifier currentModifier)){
            currentModifier = default;
            currentModifier.Percent = 0f;
        }
        currentModifier.Percent += statModifier.Percent * currentModifier.Percent * 0.001f;
        currentModifier.Percent += statModifier.Percent;
        currentModifier.Flat += statModifier.Flat;

        map[stat] = currentModifier;
    }
    public Effect Stacked(int times)
    {
        return new Effect(stat,statModifier*times,targetTags);
    }
}
