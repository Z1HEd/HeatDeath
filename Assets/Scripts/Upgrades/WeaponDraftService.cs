using System.Collections.Generic;
using UnityEngine;

public class WeaponDraftService
{
    private readonly List<WeaponDefinition> definitions;
    private readonly System.Random random;

    public WeaponDraftService(int? seed = null)
    {
        definitions = new List<WeaponDefinition>(Resources.LoadAll<WeaponDefinition>("Weapons"));
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    // Returns up to optionCount random uninstalled weapons.
    public List<WeaponDefinition> GetDraftOptions(Player player, int optionCount)
    {
        var result = new List<WeaponDefinition>();
        if (player == null || optionCount <= 0)
            return result;

        ModuleManager moduleManager = player.GetComponent<ModuleManager>();

        var candidates = new List<WeaponDefinition>();
        for (int i = 0; i < definitions.Count; i++)
        {
            WeaponDefinition def = definitions[i];
            if (def != null && (moduleManager == null || !moduleManager.HasModule(def)))
                candidates.Add(def);
        }

        int target = Mathf.Min(optionCount, candidates.Count);
        for (int i = 0; i < target; i++)
        {
            int pick = random.Next(candidates.Count);
            result.Add(candidates[pick]);
            candidates.RemoveAt(pick);
        }

        return result;
    }
}
