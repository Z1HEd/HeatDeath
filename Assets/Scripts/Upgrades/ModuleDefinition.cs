using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ModuleDefinition", menuName = "HeatDeath/Module Definition")]
public class ModuleDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string componentTypeName;
    [SerializeField] private List<TagDefinition> tags = new List<TagDefinition>();

    public string DisplayName => displayName;
    public string ComponentTypeName => componentTypeName;
    public IReadOnlyList<TagDefinition> Tags => tags;

    public bool HasTag(TagDefinition tag)
    {
        return tag != null && tags.Contains(tag);
    }

    public bool MatchesAnyTag(IReadOnlyList<TagDefinition> targetTags)
    {
        if (targetTags == null || targetTags.Count == 0)
            return false;

        for (int i = 0; i < targetTags.Count; i++)
        {
            TagDefinition target = targetTags[i];
            if (target != null && tags.Contains(target))
                return true;
        }

        return false;
    }

    public Type ResolveComponentType()
    {
        Type direct = Type.GetType(componentTypeName);
        if (direct != null)
            return direct;

        return Type.GetType(componentTypeName + ", Assembly-CSharp");
    }
}
