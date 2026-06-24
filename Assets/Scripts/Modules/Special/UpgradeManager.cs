using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class UpgradeManager : MonoBehaviour
{
    private Ship ship;
    private readonly Dictionary<UpgradeDefinition, int> stackCounts = new Dictionary<UpgradeDefinition, int>();
    private readonly Dictionary<UpgradeDefinition,List<Effect>> relatedEffects = new Dictionary<UpgradeDefinition, List<Effect>>();
    public void Awake()
    {
        ship = GetComponent<Ship>();

    }
    public bool CanAddUpgrade(UpgradeDefinition upgrade)
    {
        int current = GetStackCount(upgrade);
        return !upgrade.IsMaxStacks(current);
    }
    public void AddUpgrade(UpgradeDefinition upgrade, int stackCount = 1)
    {
        if (!CanAddUpgrade(upgrade))
        {
            Debug.LogWarning("Cannot add upgrade. Forgot to check CanAddUpgrade()?.");
            return;
        }

        int current = GetStackCount(upgrade);
        stackCounts[upgrade] = current + stackCount;
        UpdateRelatedEffects(upgrade);
    }
    public void ClearAll()
    {
        if (stackCounts.Count == 0)
            return;

        stackCounts.Clear();
        ClearRelatedEffects();
    }
    private void ClearRelatedEffects()
    {
        foreach (var upgrade in relatedEffects.Keys)
            foreach (var effect in relatedEffects[upgrade])
                ship.effectManager.RemoveEffect(effect);
    }

    private void UpdateRelatedEffects(UpgradeDefinition upgrade)
    {
        if (relatedEffects.ContainsKey(upgrade))
            foreach (var effect in relatedEffects[upgrade])
                ship.effectManager.RemoveEffect(effect);
        int stacks = stackCounts[upgrade];
        List<Effect> newEffects = new List<Effect>();
        foreach (var effect in upgrade.Effects)
        {
            newEffects.Add(effect.Stacked(stacks));
        }
        foreach (var newEffect in newEffects)
        {
            ship.effectManager.AddEffect(newEffect);
        }
        relatedEffects[upgrade] = newEffects;
    }

    public int GetStackCount(UpgradeDefinition upgrade)
    {
        return stackCounts.TryGetValue(upgrade, out int count) ? count : 0;
    }

    public bool HasUpgrade(UpgradeDefinition upgrade)
    {
        return GetStackCount(upgrade) > 0;
    }

    public bool IsFullUpgraded(ModuleDefinition module)
    {
        foreach (var upgradeStack in stackCounts)
        {
            UpgradeDefinition upgrade = upgradeStack.Key;
            if (!upgrade.IsBoundTo(module))
                continue;

            if (!upgrade.IsMaxStacks(upgradeStack.Value))
                return false;
        }

        return true;
    }

    
}