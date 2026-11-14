using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class UpgradeManager : MonoBehaviour
{
    // Slots per module before evolution. Evolving opens one more, for its path's pool.
    public const int BaseSlots = 3;

    // The slots of one module in gameplay terms: a weapon, a ship module, or the core.
    private class SlotGroup
    {
        public readonly List<UpgradeDefinition> baseSlots = new List<UpgradeDefinition>();
        public UpgradeDefinition evolutionSlot;
        public EvolutionPathDefinition path;
    }

    private Ship ship;
    private readonly Dictionary<UpgradeDefinition, int> stackCounts = new Dictionary<UpgradeDefinition, int>();
    // Keyed by the installed module's definition; for the core, the ship's own core.
    private readonly Dictionary<ModuleDefinition, SlotGroup> groups = new Dictionary<ModuleDefinition, SlotGroup>();

    public void Awake()
    {
        ship = GetComponent<Ship>();
    }

    // The installed module whose slots the upgrade fills: its bound module, or the ship's core when the upgrade
    // is bound to the shared core. Null while neither is installed.
    public ModuleDefinition GroupOf(UpgradeDefinition upgrade) => ship.moduleManager.FindInstalled(upgrade.BoundModule);

    // Whether the upgrade can be taken now. The single rule for what drafts may offer.
    public bool CanTake(UpgradeDefinition upgrade)
    {
        if (upgrade == null)
            return false;

        ModuleDefinition group = GroupOf(upgrade);
        if (group == null || upgrade.IsMaxStacks(GetStackCount(upgrade)) || HasMutuallyExclusiveUpgradeEquipped(upgrade))
            return false;

        if (HasUpgrade(upgrade))
            return true;

        SlotGroup slots = GetSlots(group);
        switch (upgrade.Role)
        {
            case UpgradeRole.Base:
                return slots.baseSlots.Count < BaseSlots;
            case UpgradeRole.Evolution:
                return slots.path != null && upgrade.EvolutionPath == slots.path && slots.evolutionSlot == null;
            default:
                return slots.path != null;
        }
    }

    public void AddUpgrade(UpgradeDefinition upgrade)
    {
        if (!CanTake(upgrade))
        {
            Debug.LogWarning($"Cannot add upgrade {upgrade.DisplayName}. Forgot to check CanTake()?");
            return;
        }

        if (!HasUpgrade(upgrade))
            FillSlot(upgrade);

        stackCounts[upgrade] = GetStackCount(upgrade) + 1;
        UpdateRelatedEffects(upgrade);
    }

    // Infinite upgrades take no slot.
    private void FillSlot(UpgradeDefinition upgrade)
    {
        SlotGroup slots = GetSlots(GroupOf(upgrade));
        if (upgrade.Role == UpgradeRole.Base)
            slots.baseSlots.Add(upgrade);
        else if (upgrade.Role == UpgradeRole.Evolution)
            slots.evolutionSlot = upgrade;
    }

    public void ClearAll()
    {
        foreach (var upgrade in stackCounts.Keys)
            ship.effectManager.Remove(upgrade);

        stackCounts.Clear();
        groups.Clear();
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

    // Modules that haven't evolved, have paths, and hold max-stack upgrades in all base slots.
    // Skips the core until core evolution exists (see Evolve); otherwise drafts would keep offering it.
    public List<ModuleDefinition> GetGroupsReadyToEvolve()
    {
        var ready = new List<ModuleDefinition>();
        foreach (var pair in groups)
        {
            SlotGroup slots = pair.Value;
            if (!IsCore(pair.Key) && slots.path == null && pair.Key.EvolutionPaths.Count > 0
                && slots.baseSlots.Count == BaseSlots
                && slots.baseSlots.TrueForAll(upgrade => upgrade.IsMaxStacks(GetStackCount(upgrade))))
                ready.Add(pair.Key);
        }
        return ready;
    }

    // Opens the 4th slot for the path's pool and unlocks the module's infinite upgrade.
    public void Evolve(ModuleDefinition group, EvolutionPathDefinition path)
    {
        if (path == null || !group.EvolutionPaths.Contains(path))
        {
            Debug.LogWarning($"{group.DisplayName} has no evolution path {(path != null ? path.DisplayName : "null")}");
            return;
        }

        // Stub: a ship's core will evolve by rebuilding the ship from the path's evolved ship prefab.
        // GetGroupsReadyToEvolve skips the core, so nothing gets here yet.
        if (IsCore(group))
        {
            Debug.LogError($"Core evolution ({path.DisplayName}) isn't implemented yet.");
            return;
        }

        GetSlots(group).path = path;
        if (path.EvolvedPrefab != null)
            ship.moduleManager.ReplaceModule(group, path.EvolvedPrefab);
    }

    private bool IsCore(ModuleDefinition group) => ship.CoreModule != null && group == ship.CoreModule.ModuleDefinition;

    public int GetUsedBaseSlots(ModuleDefinition group) => GetSlots(group).baseSlots.Count;

    // The path the module evolved along, or null.
    public EvolutionPathDefinition GetPath(ModuleDefinition group) => GetSlots(group).path;

    private SlotGroup GetSlots(ModuleDefinition group)
    {
        if (!groups.TryGetValue(group, out SlotGroup slots))
            groups[group] = slots = new SlotGroup();
        return slots;
    }

    private bool HasMutuallyExclusiveUpgradeEquipped(UpgradeDefinition upgrade)
    {
        foreach (var owned in stackCounts.Keys)
        {
            if (owned.IsMutuallyExclusiveWith(upgrade) || upgrade.IsMutuallyExclusiveWith(owned))
                return true;
        }

        return false;
    }
}
