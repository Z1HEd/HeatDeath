using System;
using System.Collections.Generic;
using UnityEngine;

public enum ModifierOperation
{
    AddFlat = 0,
    AddPercent = 1
}

[Serializable]
public struct StatModifier
{
    [SerializeField] public StatType stat;
    [SerializeField] public ModifierOperation operation;
    [SerializeField] public float value;
    [SerializeField] public List<TagDefinition> targetTags;

    public bool HasTargetTags => targetTags != null && targetTags.Count > 0;

    public bool MatchesModule(ModuleDefinition moduleDefinition)
    {
        if (moduleDefinition == null || targetTags == null || targetTags.Count == 0)
            return false;

        return moduleDefinition.MatchesAnyTag(targetTags);
    }
}
