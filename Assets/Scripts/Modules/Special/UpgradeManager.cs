using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class UpgradeManager : MonoBehaviour
{
    private Ship ship;
    private readonly Dictionary<UpgradeDefinition, int> stackCounts = new Dictionary<UpgradeDefinition, int>();
    public void Awake()
    {
        ship = GetComponent<Ship>();

    }
    public bool CanAddUpgrade(UpgradeDefinition upgrade)
    {
        int current = GetStackCount(upgrade);
        return !upgrade.IsMaxStacks(current);
    }

    public bool HasMutuallyExclusiveUpgradeEquipped(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return false;

        foreach (var owned in stackCounts.Keys)
        {
            if (owned.IsMutuallyExclusiveWith(upgrade) || upgrade.IsMutuallyExclusiveWith(owned))
                return true;
        }

        return false;
    }

    public void AddUpgrade(UpgradeDefinition upgrade, int stackCount = 1)
    {
        if (!CanAddUpgrade(upgrade))
        {
            Debug.LogWarning("Cannot add upgrade. Forgot to check CanAddUpgrade()?.");
            return;
        }

        int newCount = GetStackCount(upgrade) + stackCount;
        stackCounts[upgrade] = upgrade.MaxStacks >= 1 ? Mathf.Min(newCount, upgrade.MaxStacks) : newCount;
        UpdateRelatedEffects(upgrade);
    }
    public void ClearAll()
    {
        foreach (var upgrade in stackCounts.Keys)
            ship.effectManager.Remove(upgrade);
        stackCounts.Clear();
    }

    // The upgrade is the effects' source, so the new copies replace those of the old stack count.
    private void UpdateRelatedEffects(UpgradeDefinition upgrade)
    {
        int stacks = stackCounts[upgrade];
        var copies = new List<Effect>();
        foreach (var effect in upgrade.Effects)
            copies.Add(effect.Copy(stacks));
        ship.effectManager.Apply(upgrade, copies);
    }

    public int GetStackCount(UpgradeDefinition upgrade)
    {
        return stackCounts.TryGetValue(upgrade, out int count) ? count : 0;
    }

    public bool HasUpgrade(UpgradeDefinition upgrade)
    {
        return GetStackCount(upgrade) > 0;
    }

    // Evolution check: every capped upgrade bound to the module is at max stacks, owned or not.
    // Infinite upgrades are skipped, so they never block evolution.
    public bool AreCappedUpgradesMaxed(ModuleDefinition module, IEnumerable<UpgradeDefinition> allUpgrades)
    {
        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null || !upgrade.IsBoundTo(module) || upgrade.IsInfinite)
                continue;

            if (!upgrade.IsMaxStacks(GetStackCount(upgrade)))
                return false;
        }

        return true;
    }

    
}