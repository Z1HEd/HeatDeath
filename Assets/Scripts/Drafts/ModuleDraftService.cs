using System.Collections.Generic;
using UnityEngine;

// Drafts modules of one kind, such as weapons or ship modules, from a Resources folder.
public class ModuleDraftService<T> where T : ModuleDefinition
{
    private readonly List<T> definitions;
    private readonly System.Random random;

    public ModuleDraftService(string resourcesFolder, int? seed = null)
    {
        definitions = new List<T>(Resources.LoadAll<T>(resourcesFolder));
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    // Returns up to optionCount random modules the player hasn't installed.
    public List<T> GetDraftOptions(Player player, int optionCount)
    {
        var result = new List<T>();
        if (player == null || optionCount <= 0)
            return result;

        ModuleManager moduleManager = player.GetComponent<ModuleManager>();

        var candidates = new List<T>();
        for (int i = 0; i < definitions.Count; i++)
        {
            T def = definitions[i];
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
