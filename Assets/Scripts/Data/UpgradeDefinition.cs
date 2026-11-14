using System.Collections.Generic;
using UnityEngine;
using System;

public enum UpgradeRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

public enum UpgradeRole
{
    Base = 0,       // Fills one of the module's 3 slots.
    Evolution = 1,  // Fills the 4th slot, which evolving along its path opens.
    Infinite = 2    // Takes no slot and stacks without limit; offered only once the module evolved.
}

[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "HeatDeath/Upgrade Definition")]
[Serializable]
public class UpgradeDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string key;
    [SerializeField, TextArea(2, 4)] private string description;
    [Tooltip("The module this upgrade is for: a weapon, a ship module, one ship's core, or the shared core for every " +
             "ship. It's offered only while that module is installed, and fills its slots.")]
    [SerializeField] private ModuleDefinition boundModule;
    [SerializeField] private UpgradeRole role;
    [Tooltip("Evolution upgrades only: the path whose 4th slot this fills.")]
    [SerializeField] private EvolutionPathDefinition evolutionPath;
    [SerializeField] private UpgradeRarity rarity;
    [Tooltip("Ignored by infinite upgrades.")]
    [SerializeField] private int maxStacks = 1;
    [SerializeField] private List<UpgradeDefinition> mutuallyExclusiveWith = new List<UpgradeDefinition>();
    
    [SerializeReference] [SubclassSelector] private List<Effect> effects = new List<Effect>();

    public string DisplayName => displayName;
    public string Key => key;
    public string Description => description;
    public ModuleDefinition BoundModule => boundModule;
    public UpgradeRole Role => role;
    public EvolutionPathDefinition EvolutionPath => evolutionPath;
    public UpgradeRarity Rarity => rarity;
    public int MaxStacks => maxStacks;
    public bool IsInfinite => role == UpgradeRole.Infinite;
    public IReadOnlyList<Effect> Effects => effects;

    public IReadOnlyList<UpgradeDefinition> MutuallyExclusiveWith => mutuallyExclusiveWith;

    public bool IsMutuallyExclusiveWith(UpgradeDefinition other)
    {
        if (other == null || other == this)
            return false;

        return mutuallyExclusiveWith.Contains(other);
    }

    public bool IsMaxStacks(int currentCount)
    {
        if (IsInfinite)
            return false;

        return currentCount >= maxStacks;
    }
}
