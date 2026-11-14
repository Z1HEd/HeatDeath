#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor-only checks of upgrade and evolution content. Runs from the menu, and whenever drafts load in the editor.
public static class UpgradeValidation
{
    [MenuItem("HeatDeath/Validate Upgrades")]
    private static void RunFromMenu()
    {
        if (Run(Resources.LoadAll<UpgradeDefinition>("Upgrades")) == 0)
            Debug.Log("Upgrade validation: no problems.");
    }

    // Logs a warning per problem and returns how many there were.
    public static int Run(IReadOnlyList<UpgradeDefinition> upgrades)
    {
        var problems = new List<string>();
        ModuleDefinition[] modules = Resources.LoadAll<ModuleDefinition>("");

        // Every player ship's own core, and the shared cores they build on.
        var shipCores = new HashSet<ModuleDefinition>();
        foreach (var shipDefinition in Resources.LoadAll<ShipDefinition>("Ships"))
        {
            // On a child, as Ship finds it.
            var core = shipDefinition.ShipPrefab != null ? shipDefinition.ShipPrefab.GetComponentInChildren<ShipCoreModule>(true) : null;
            if (core == null || core.ModuleDefinition == null)
                problems.Add($"{shipDefinition.name}: its prefab has no core definition");
            else if (shipCores.Add(core.ModuleDefinition) && core.ModuleDefinition.BaseDefinition == null)
                problems.Add($"{core.ModuleDefinition.name}: no base core, so {shipDefinition.name} never sees shared core upgrades");
        }
        var sharedCores = new HashSet<ModuleDefinition>(shipCores.Select(c => c.BaseDefinition).Where(b => b != null));
        bool IsCore(ModuleDefinition definition) => shipCores.Contains(definition) || sharedCores.Contains(definition);

        foreach (var module in modules)
        {
            if (module.BaseDefinition != null && !shipCores.Contains(module))
                problems.Add($"{module.name}: only a ship's core may have a base definition");
            if (IsCore(module) && module.EvolutionPaths.Count > 0)
                problems.Add($"{module.name}: core evolution isn't implemented yet, so cores can't have paths");
            foreach (var path in module.EvolutionPaths)
                CheckPath(module, path, problems);
        }

        foreach (var listings in modules.SelectMany(m => m.EvolutionPaths.Where(p => p != null).Select(p => (module: m, path: p)))
                                        .GroupBy(entry => entry.path).Where(g => g.Count() > 1))
            problems.Add($"{listings.Key.name}: listed by {string.Join(", ", listings.Select(e => e.module.name))}");

        var infiniteCounts = new Dictionary<ModuleDefinition, int>();
        foreach (var upgrade in upgrades)
        {
            ModuleDefinition bound = upgrade.BoundModule;
            if (bound == null)
            {
                problems.Add($"{upgrade.name}: no bound module");
                continue;
            }

            if (!(bound is WeaponDefinition || bound is ShipModuleDefinition || IsCore(bound)))
                problems.Add($"{upgrade.name}: bound to {bound.name}; bind it to a weapon, a ship module, a ship's core, or the shared core");
            if (!upgrade.IsInfinite && upgrade.MaxStacks < 1)
                problems.Add($"{upgrade.name}: max stacks below 1");

            if (upgrade.Role == UpgradeRole.Evolution)
            {
                if (upgrade.EvolutionPath == null)
                    problems.Add($"{upgrade.name}: evolution upgrade without a path");
                else if (!bound.EvolutionPaths.Contains(upgrade.EvolutionPath))
                    problems.Add($"{upgrade.name}: path {upgrade.EvolutionPath.name} isn't listed on {bound.name}");
            }
            else if (upgrade.EvolutionPath != null)
                problems.Add($"{upgrade.name}: has a path but isn't an evolution upgrade");

            if (upgrade.IsInfinite)
            {
                if (IsCore(bound))
                    problems.Add($"{upgrade.name}: the core has no infinite upgrade");
                infiniteCounts[bound] = infiniteCounts.GetValueOrDefault(bound) + 1;
            }
        }
        foreach (var pair in infiniteCounts.Where(p => p.Value > 1))
            problems.Add($"{pair.Key.name}: {pair.Value} infinite upgrades, at most 1 allowed");

        foreach (var problem in problems)
            Debug.LogWarning($"Upgrade validation: {problem}");
        return problems.Count;
    }

    private static void CheckPath(ModuleDefinition module, EvolutionPathDefinition path, List<string> problems)
    {
        if (path == null)
        {
            problems.Add($"{module.name}: empty evolution path entry");
            return;
        }

        ModuleBase evolved = path.EvolvedPrefab != null ? path.EvolvedPrefab.GetComponent<ModuleBase>() : null;
        if (evolved == null)
            problems.Add($"{path.name}: no evolved prefab with a module on its root");
        else if (evolved.ModuleDefinition != module)
            problems.Add($"{path.name}: the evolved prefab must keep {module.name}'s definition, so upgrades carry over");
    }
}
#endif
