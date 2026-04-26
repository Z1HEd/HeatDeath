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

    private static readonly Dictionary<UpgradeRarity, int> RarityWeights =
        new Dictionary<UpgradeRarity, int>
        {
            { UpgradeRarity.Common,    70 },
            { UpgradeRarity.Rare,      20 },
            { UpgradeRarity.Epic,       8 },
            { UpgradeRarity.Legendary,  2 },
        };

    private readonly List<UpgradeDefinition> definitions;
    private readonly System.Random random;

    public UpgradeDraftService(int? seed = null)
    {
        definitions = new List<UpgradeDefinition>(Resources.LoadAll<UpgradeDefinition>("Upgrades"));
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public List<UpgradeDefinition> GetDraftOptions(Player player, int optionCount)
    {
        var result = new List<UpgradeDefinition>();
        if (player == null || optionCount <= 0)
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
        if (player == null)
            return result;

        for (int i = 0; i < definitions.Count; i++)
        {
            UpgradeDefinition upgrade = definitions[i];
            if (IsEligibleForDraft(upgrade, player))
                result.Add(upgrade);
        }

        return result;
    }

    private static bool IsEligibleForDraft(UpgradeDefinition upgrade, Player player)
    {
        if (upgrade == null || player == null)
            return false;

        ModuleManager moduleManager = player.moduleManager;
        UpgradeManager upgradeManager = player.GetComponent<UpgradeManager>();

        if (moduleManager == null || upgradeManager == null)
            return false;

        if (!upgradeManager.CanAddUpgrade(upgrade))
            return false;

        return upgrade.BoundModule != null
            ? moduleManager.HasModule(upgrade.BoundModule)
            : IsApplicableToAnyInstalledModule(upgrade, moduleManager);
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

    private static bool IsApplicableToAnyInstalledModule(UpgradeDefinition upgrade, ModuleManager moduleManager)
    {
        if (upgrade == null || moduleManager == null)
            return false;

        HashSet<ModuleDefinition> installedModules = moduleManager.GetInstalledModuleDefinitions();
        if (installedModules.Count == 0)
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
        int total = RarityWeights[UpgradeRarity.Common]
                  + RarityWeights[UpgradeRarity.Rare]
                  + RarityWeights[UpgradeRarity.Epic]
                  + RarityWeights[UpgradeRarity.Legendary];
        if (total <= 0)
            return UpgradeRarity.Common;

        int roll = random.Next(total);
        int common = RarityWeights[UpgradeRarity.Common];
        int rare   = RarityWeights[UpgradeRarity.Rare];
        int epic   = RarityWeights[UpgradeRarity.Epic];
        if (roll < common)               return UpgradeRarity.Common;
        if (roll < common + rare)        return UpgradeRarity.Rare;
        if (roll < common + rare + epic) return UpgradeRarity.Epic;
        return UpgradeRarity.Legendary;
    }

    private UpgradeRarity[] GetRarityFallbackOrder(UpgradeRarity rolledRarity)
    {
        return RarityFallbackMap.TryGetValue(rolledRarity, out UpgradeRarity[] value)
            ? value
            : RarityFallbackMap[UpgradeRarity.Common];
    }
}
