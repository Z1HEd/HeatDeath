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

[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "HeatDeath/Upgrade Definition")]
[Serializable]
public class UpgradeDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string key;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private ModuleDefinition boundModule;
    [SerializeField] private UpgradeRarity rarity;
    [SerializeField] private int maxStacks = 1;
    [SerializeField] private List<Effect> effects = new List<Effect>();

    public string DisplayName => displayName;
    public string Key => key;
    public string Description => description;
    public ModuleDefinition BoundModule => boundModule;
    public bool IsGeneral => boundModule == null;
    public UpgradeRarity Rarity => rarity;
    public int MaxStacks => maxStacks;
    public IReadOnlyList<Effect> Effects => effects;

    public bool IsBoundTo(ModuleDefinition module)
    {
        return module != null && boundModule == module;
    }

    public bool IsMaxStacks(int currentCount)
    {
        if (maxStacks < 1)
            return false;

        return currentCount >= maxStacks;
    }
    public bool IsApplicableTo(ModuleDefinition module)
    {
        if (boundModule == module) return true;
        foreach (var effect in effects)
        {
            if (effect.IsApplicableTo(module)) return true;
        }
        return false;
    }
}
