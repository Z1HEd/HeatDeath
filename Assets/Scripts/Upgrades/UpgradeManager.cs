using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class UpgradeManager : MonoBehaviour
{
    private readonly Dictionary<UpgradeDefinition, int> stackCounts = new Dictionary<UpgradeDefinition, int>();

    public event Action OnChanged;

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

        List<ModuleBase> modules = moduleManager.GetModules<ModuleBase>();
        for (int i = 0; i < modules.Count; i++)
        {
            ModuleBase module = modules[i];
            if (module == null)
                continue;

            module.Recalculate();
        }
    }
}
