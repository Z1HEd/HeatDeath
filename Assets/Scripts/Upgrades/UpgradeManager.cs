using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class UpgradeManager : MonoBehaviour
{
    private readonly Dictionary<UpgradeDefinition, int> stackCounts = new Dictionary<UpgradeDefinition, int>();
    private readonly Dictionary<StatDefinition, StatModifier> statModifiers = new Dictionary<StatDefinition, StatModifier>();

    public event Action OnChanged;
    public IReadOnlyDictionary<StatDefinition, StatModifier> StatModifiers => statModifiers;

    public bool CanAddUpgrade(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return false;

        int current = GetStackCount(upgrade);
        return upgrade.CanStackFromCount(current);
    }

    public void AddUpgrade(UpgradeDefinition upgrade)
    {
        if (!CanAddUpgrade(upgrade))
        {
            Debug.LogWarning("Cannot add upgrade. Draft filtering should prevent this case.", this);
            return;
        }

        int current = GetStackCount(upgrade);
        stackCounts[upgrade] = current + 1;
        RecalculateAllModules();
        OnChanged?.Invoke();
    }

    public int GetStackCount(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return 0;

        return stackCounts.TryGetValue(upgrade, out int count) ? count : 0;
    }

    public bool HasUpgrade(UpgradeDefinition upgrade)
    {
        return GetStackCount(upgrade) > 0;
    }

    public IReadOnlyDictionary<UpgradeDefinition, int> GetAllUpgrades()
    {
        return stackCounts;
    }

    // Converts runtime upgrade references to stable keys for save/export payloads.
    public Dictionary<string, int> BuildKeySnapshot()
    {
        var snapshot = new Dictionary<string, int>();
        foreach (var pair in stackCounts)
        {
            if (pair.Key == null || string.IsNullOrWhiteSpace(pair.Key.Key) || pair.Value <= 0)
                continue;

            snapshot[pair.Key.Key] = pair.Value;
        }

        return snapshot;
    }

    public void ClearAll()
    {
        if (stackCounts.Count == 0)
            return;

        stackCounts.Clear();
        RecalculateAllModules();
        OnChanged?.Invoke();
    }

    private void RecalculateAllModules()
    {
        ModuleManager moduleManager = GetComponent<ModuleManager>();
        if (moduleManager == null)
            return;

        RebuildCombinedStatModifiers();

        List<ModuleBase> modules = moduleManager.GetModules<ModuleBase>();
        for (int i = 0; i < modules.Count; i++)
        {
            ModuleBase module = modules[i];
            if (module == null)
                continue;

            module.Recalculate();
        }
    }

    public void RebuildCombinedStatModifiers()
    {
        statModifiers.Clear();

        foreach (var pair in stackCounts)
        {
            UpgradeDefinition upgrade = pair.Key;
            int stacks = pair.Value;
            if (upgrade == null || stacks <= 0)
                continue;

            IReadOnlyList<UpgradeEffect> effects = upgrade.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                UpgradeEffect effect = effects[i];
                if (effect.stat == null)
                    continue;

                AddModifier(statModifiers, effect.stat, effect.operation, effect.value * stacks);
            }
        }
    }

    private static void AddModifier(Dictionary<StatDefinition, StatModifier> map, StatDefinition stat, UpgradeEffectOperation operation, float amount)
    {
        if (!map.TryGetValue(stat, out StatModifier modifier))
            modifier = default;

        if (operation == UpgradeEffectOperation.AddPercent)
            modifier.Percent += amount;
        else
            modifier.Flat += amount;

        map[stat] = modifier;
    }
}
