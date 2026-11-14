using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// One upgrade-draft option: an upgrade, or evolving a module along one of its paths.
public class UpgradeDraftOption
{
    public UpgradeDefinition Upgrade { get; }
    public ModuleDefinition Module { get; }
    public EvolutionPathDefinition Path { get; }
    public bool IsEvolution => Path != null;

    public UpgradeDraftOption(UpgradeDefinition upgrade) => Upgrade = upgrade;

    public UpgradeDraftOption(ModuleDefinition module, EvolutionPathDefinition path)
    {
        Module = module;
        Path = path;
    }
}

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
#if UNITY_EDITOR
        UpgradeValidation.Run(definitions);
#endif
    }

    // Each option's chances: that it offers an evolution (while a module is ready), and otherwise that it stacks
    // an owned upgrade rather than offering a new one. One option leans to stacking and one to new upgrades,
    // both rarely evolving; any others are free, even on stacking and likelier to evolve. Positions are shuffled.
    private static readonly (float stack, float evolution)[] SkewedSlots = { (0.8f, 0.2f), (0.2f, 0.2f) };
    private static readonly (float stack, float evolution) FreeSlot = (0.5f, 0.6f);

    public List<UpgradeDraftOption> GetDraftOptions(Player player, int optionCount)
    {
        var result = new List<UpgradeDraftOption>();
        if (player == null || optionCount <= 0)
            return result;

        UpgradeManager upgrades = player.upgradeManager;
        List<UpgradeDefinition> candidates = definitions.FindAll(upgrades.CanTake);
        var evolutions = new List<UpgradeDraftOption>();
        foreach (var module in upgrades.GetGroupsReadyToEvolve())
            foreach (var path in module.EvolutionPaths)
                evolutions.Add(new UpgradeDraftOption(module, path));

        foreach (var slot in ShuffledSlots(optionCount))
        {
            // With no upgrades left, an evolution fills the option.
            if (evolutions.Count > 0 && (random.NextDouble() < slot.evolution || candidates.Count == 0))
            {
                UpgradeDraftOption evolution = PickEvolution(evolutions);
                result.Add(evolution);
                evolutions.Remove(evolution);
                continue;
            }

            bool stack = random.NextDouble() < slot.stack;
            List<UpgradeDefinition> pool = candidates.FindAll(u => upgrades.HasUpgrade(u) == stack);
            // None of the drawn kind: all candidates left are of the other kind.
            if (pool.Count == 0)
                pool = candidates;
            if (pool.Count == 0)
                break;

            UpgradeDefinition picked = PickFromRandomGroup(pool, upgrades);
            result.Add(new UpgradeDraftOption(picked));
            candidates.Remove(picked);
        }

        return result;
    }

    private List<(float stack, float evolution)> ShuffledSlots(int count)
    {
        var slots = new List<(float stack, float evolution)>();
        for (int i = 0; i < count; i++)
            slots.Add(i < SkewedSlots.Length ? SkewedSlots[i] : FreeSlot);

        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }
        return slots;
    }

    // A ready module first, uniformly, then one of its paths not offered yet.
    private UpgradeDraftOption PickEvolution(List<UpgradeDraftOption> evolutions)
    {
        List<ModuleDefinition> modules = evolutions.Select(e => e.Module).Distinct().ToList();
        ModuleDefinition module = modules[random.Next(modules.Count)];
        List<UpgradeDraftOption> paths = evolutions.FindAll(e => e.Module == module);
        return paths[random.Next(paths.Count)];
    }

    // A module first, uniformly among those in the pool, then a rarity among its upgrades.
    private UpgradeDefinition PickFromRandomGroup(List<UpgradeDefinition> pool, UpgradeManager upgrades)
    {
        var groups = new List<ModuleDefinition>();
        foreach (var upgrade in pool)
        {
            ModuleDefinition group = upgrades.GroupOf(upgrade);
            if (!groups.Contains(group))
                groups.Add(group);
        }

        ModuleDefinition chosen = groups[random.Next(groups.Count)];
        return PickWithRarityFallback(pool.FindAll(u => upgrades.GroupOf(u) == chosen), RollRarity());
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
