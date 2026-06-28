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
    public StatModifierEffect(StatType Stat, StatModifier Modifier, List<TagDefinition> TargetTags)
    {
        stat = Stat;
        statModifier = Modifier;
        targetTags = TargetTags;
    }

    public override bool IsApplicableTo(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.MatchesAnyTag(targetTags);
    }

    public override void ApplyToShip(Ship ship){}
    public override void RemoveFromShip(Ship ship){}

    public override void ApplyToModule(ModuleBase module)
    {
        var map = module.currentModifiers;
        if (!map.TryGetValue(stat, out StatModifier currentModifier)){
            currentModifier = default;
            currentModifier.Percent = 0f;
        }
        currentModifier.Percent += statModifier.Percent * currentModifier.Percent * 0.001f;
        currentModifier.Percent += statModifier.Percent;
        currentModifier.Flat += statModifier.Flat;

        map[stat] = currentModifier;
    }
    public override Effect Stacked(float times)
    {
        return new StatModifierEffect(stat,statModifier*times,targetTags);
    }
}
