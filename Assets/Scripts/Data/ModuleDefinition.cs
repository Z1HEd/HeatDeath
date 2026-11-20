using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ModuleDefinition", menuName = "HeatDeath/Module Definition")]
public class ModuleDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private List<TagDefinition> tags = new List<TagDefinition>();
    [Tooltip("The paths this module can evolve along. A ship's paths go on its own core.")]
    [SerializeField] private List<EvolutionPathDefinition> evolutionPaths = new List<EvolutionPathDefinition>();
    [Tooltip("Ship cores only: the shared core this one builds on. Upgrades bound to the shared core are offered " +
             "to every ship whose core builds on it, and fill that ship's core slots.")]
    [SerializeField] private ModuleDefinition baseDefinition;

    public string DisplayName => displayName;
    public IReadOnlyList<TagDefinition> Tags => tags;
    public IReadOnlyList<EvolutionPathDefinition> EvolutionPaths => evolutionPaths;
    public ModuleDefinition BaseDefinition => baseDefinition;

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
}
