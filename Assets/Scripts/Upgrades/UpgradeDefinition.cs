using System.Collections.Generic;
using UnityEngine;

public enum UpgradeRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "HeatDeath/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string key;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private ModuleDefinition targetModule;
    [SerializeField] private UpgradeRarity rarity;
    [SerializeField] private int maxStacks = 1;
    [SerializeField] private List<UpgradeEffect> effects = new List<UpgradeEffect>();

    public string DisplayName => displayName;
    public string Key => key;
    public string Description => description;
    public ModuleDefinition TargetModule => targetModule;
    public UpgradeRarity Rarity => rarity;
    public int MaxStacks => maxStacks;
    public IReadOnlyList<UpgradeEffect> Effects => effects;

    public bool CanStackFromCount(int currentCount)
    {
        if (maxStacks < 1)
            return true;

        return currentCount < maxStacks;
    }
}
