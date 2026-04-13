using System;
using System.Collections.Generic;
using UnityEngine;

public struct ActiveStatModifier
{
    public StatModifier Modifier;
    public bool IsUpgradeBound;
    public ModuleDefinition BoundModule;

    public bool AppliesToModule(ModuleDefinition module)
    {
        if (module == null || Modifier.stat == StatType.None)
            return false;

        if (IsUpgradeBound)
        {
            if (BoundModule == null || module != BoundModule)
                return false;

            if (!Modifier.HasTargetTags)
                return true;

            return Modifier.MatchesModule(module);
        }

        if (!Modifier.HasTargetTags)
            return false;

        return Modifier.MatchesModule(module);
    }
}

[RequireComponent(typeof(Ship))]
public class UpgradeManager : MonoBehaviour
{
    private readonly Dictionary<UpgradeDefinition, int> stackCounts = new Dictionary<UpgradeDefinition, int>();
    private readonly List<ActiveStatModifier> activeEffects = new List<ActiveStatModifier>();

    public event Action OnChanged;
    public IReadOnlyList<ActiveStatModifier> ActiveEffects => activeEffects;

    public bool CanAddUpgrade(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return false;

        int current = GetStackCount(upgrade);
        return !upgrade.IsMaxStacks(current);
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

    public bool IsFullUpgraded(ModuleDefinition module, IReadOnlyList<UpgradeDefinition> allUpgrades)
    {
        if (module == null || allUpgrades == null)
            return false;

        bool hasBoundUpgrade = false;
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeDefinition upgrade = allUpgrades[i];
            if (upgrade == null || !upgrade.IsBoundTo(module))
                continue;

            hasBoundUpgrade = true;
            if (!upgrade.IsMaxStacks(GetStackCount(upgrade)))
                return false;
        }

        return hasBoundUpgrade;
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

    public void RecalculateAllModules()
    {
        ModuleManager moduleManager = GetComponent<ModuleManager>();
        if (moduleManager == null)
            return;

        List<ModuleBase> modules = moduleManager.GetModules<ModuleBase>();
        RebuildActiveEffects();
        for (int i = 0; i < modules.Count; i++)
        {
            ModuleBase module = modules[i];
            if (module == null)
                continue;

            module.UpdateModifiers();
        }
    }

    private void RebuildActiveEffects()
    {
        activeEffects.Clear();

        foreach (var pair in stackCounts)
        {
            UpgradeDefinition upgrade = pair.Key;
            int stacks = pair.Value;
            if (upgrade == null || stacks <= 0)
                continue;

            IReadOnlyList<StatModifier> effects = upgrade.Modifiers;
            for (int i = 0; i < effects.Count; i++)
            {
                StatModifier effect = effects[i];
                if (effect.stat == StatType.None)
                    continue;

                effect.value *= stacks;
                activeEffects.Add(new ActiveStatModifier
                {
                    Modifier = effect,
                    IsUpgradeBound = upgrade.BoundModule != null,
                    BoundModule = upgrade.BoundModule
                });
            }
        }
    }
}
