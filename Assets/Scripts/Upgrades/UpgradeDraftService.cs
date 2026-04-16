using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeDraftService
{
    private static readonly Dictionary<UpgradeRarity, UpgradeRarity[]> RarityFallbackMap =
        new Dictionary<UpgradeRarity, UpgradeRarity[]>
        {
            { UpgradeRarity.Common, new[] { UpgradeRarity.Common, UpgradeRarity.Rare, UpgradeRarity.Epic, UpgradeRarity.Legendary } },
            { UpgradeRarity.Rare, new[] { UpgradeRarity.Rare, UpgradeRarity.Common, UpgradeRarity.Epic, UpgradeRarity.Legendary } },
            { UpgradeRarity.Epic, new[] { UpgradeRarity.Epic, UpgradeRarity.Rare, UpgradeRarity.Legendary, UpgradeRarity.Common } },
            { UpgradeRarity.Legendary, new[] { UpgradeRarity.Legendary, UpgradeRarity.Epic, UpgradeRarity.Rare, UpgradeRarity.Common } }
        };

    private readonly UpgradeDatabase database;
    private readonly System.Random random;

    public UpgradeDraftService(UpgradeDatabase database, int? seed = null)
    {
        this.database = database;
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public List<UpgradeDefinition> BuildUpgradeDraftOptions(Player player, int optionCount)
    {
        var result = new List<UpgradeDefinition>();
        if (database == null || player == null || optionCount <= 0)
            return result;

        List<UpgradeDefinition> candidates = GetAvailableUpgrades(player);
        if (candidates.Count == 0)
            return result;

        for (int i = 0; i < optionCount; i++)
        {
            if (candidates.Count == 0)
                break;

            UpgradeRarity rolledRarity = RollRarity();
            UpgradeDefinition picked = PickWithRarityFallback(candidates, rolledRarity);
            if (picked == null)
                break;

            result.Add(picked);
            candidates.Remove(picked);
        }

        return result;
    }

    public List<UpgradeDefinition> GetAvailableUpgrades(Player player)
    {
        var result = new List<UpgradeDefinition>();
        if (database == null || player == null)
            return result;

        ModuleManager moduleManager = player.moduleManager;
        UpgradeManager upgradeManager = player.GetComponent<UpgradeManager>();
        if (moduleManager == null || upgradeManager == null)
            return result;

        HashSet<ModuleDefinition> installedModules = moduleManager.GetInstalledModuleDefinitions();
        IReadOnlyList<UpgradeDefinition> allUpgrades = database.Upgrades;

        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeDefinition upgrade = allUpgrades[i];
            if (IsEligibleForDraft(upgrade, installedModules, upgradeManager))
                result.Add(upgrade);
        }

        return result;
    }

    private static bool IsEligibleForDraft(
        UpgradeDefinition upgrade,
        IReadOnlyCollection<ModuleDefinition> installedModules,
        UpgradeManager upgradeManager)
    {
        if (upgrade == null || upgradeManager == null)
            return false;

        if (!upgradeManager.CanAddUpgrade(upgrade))
            return false;
        
        return upgrade.BoundModule != null
            ? installedModules != null && installedModules.Contains(upgrade.BoundModule)
            : IsApplicableToAnyInstalledModule(upgrade, installedModules);
    }

    public bool HasAnyAvailableUpgradeForModule(ModuleDefinition moduleDefinition, Player player)
    {
        if (moduleDefinition == null)
            return false;

        List<UpgradeDefinition> available = GetAvailableUpgrades(player);
        for (int i = 0; i < available.Count; i++)
        {
            UpgradeDefinition upgrade = available[i];
            if (upgrade != null && upgrade.IsBoundTo(moduleDefinition))
                return true;

            if (upgrade != null && upgrade.IsGeneral && IsApplicableToModule(upgrade, moduleDefinition))
                return true;
        }

        return false;
    }

    private static bool IsApplicableToAnyInstalledModule(UpgradeDefinition upgrade, IReadOnlyCollection<ModuleDefinition> installedModules)
    {
        if (upgrade == null || installedModules == null || installedModules.Count == 0)
            return false;

        IReadOnlyList<StatModifier> effects = upgrade.Modifiers;
        for (int i = 0; i < effects.Count; i++)
        {
            StatModifier effect = effects[i];
            if (effect.stat == StatType.None)
                continue;

            foreach (ModuleDefinition module in installedModules)
            {
                if (effect.MatchesModule(module))
                    return true;
            }
        }

        return false;
    }

    private static bool IsApplicableToModule(UpgradeDefinition upgrade, ModuleDefinition moduleDefinition)
    {
        if (upgrade == null || moduleDefinition == null)
            return false;

        IReadOnlyList<StatModifier> effects = upgrade.Modifiers;
        for (int i = 0; i < effects.Count; i++)
        {
            StatModifier effect = effects[i];
            if (effect.stat == StatType.None)
                continue;

            if (effect.MatchesModule(moduleDefinition))
                return true;
        }

        return false;
    }

    private UpgradeDefinition PickWithRarityFallback(List<UpgradeDefinition> candidates, UpgradeRarity rolledRarity)
    {
        UpgradeRarity[] order = GetRarityFallbackOrder(rolledRarity);
        for (int i = 0; i < order.Length; i++)
        {
            UpgradeRarity rarity = order[i];
            List<UpgradeDefinition> bucket = FilterByRarity(candidates, rarity);
            if (bucket.Count == 0)
                continue;

            return bucket[random.Next(bucket.Count)];
        }

        return null;
    }

    private List<UpgradeDefinition> FilterByRarity(List<UpgradeDefinition> source, UpgradeRarity rarity)
    {
        var result = new List<UpgradeDefinition>();
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i].Rarity == rarity)
                result.Add(source[i]);
        }

        return result;
    }

    private UpgradeRarity RollRarity()
    {
        int common = Mathf.Max(0, database.GetRarityWeight(UpgradeRarity.Common));
        int rare = Mathf.Max(0, database.GetRarityWeight(UpgradeRarity.Rare));
        int epic = Mathf.Max(0, database.GetRarityWeight(UpgradeRarity.Epic));
        int legendary = Mathf.Max(0, database.GetRarityWeight(UpgradeRarity.Legendary));

        int total = common + rare + epic + legendary;
        if (total <= 0)
            return UpgradeRarity.Common;

        int roll = random.Next(total);
        if (roll < common)
            return UpgradeRarity.Common;
        if (roll < common + rare)
            return UpgradeRarity.Rare;
        if (roll < common + rare + epic)
            return UpgradeRarity.Epic;

        return UpgradeRarity.Legendary;
    }

    private UpgradeRarity[] GetRarityFallbackOrder(UpgradeRarity rolledRarity)
    {
        return RarityFallbackMap.TryGetValue(rolledRarity, out UpgradeRarity[] value)
            ? value
            : RarityFallbackMap[UpgradeRarity.Common];
    }
}
