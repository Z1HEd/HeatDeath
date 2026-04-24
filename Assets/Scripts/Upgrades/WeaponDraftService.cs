using System.Collections.Generic;
using UnityEngine;

public class WeaponDraftService
{
    public const string DefaultResourcesPath = "Weapons";

    private readonly string resourcesPath;
    private readonly System.Random random;

    public WeaponDraftService(string resourcesPath = DefaultResourcesPath, int? seed = null)
    {
        this.resourcesPath = string.IsNullOrWhiteSpace(resourcesPath) ? DefaultResourcesPath : resourcesPath;
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    // Loads all authored WeaponDefinition assets from Resources/<path>.
    public List<WeaponDefinition> LoadDefinitions()
    {
        WeaponDefinition[] assets = Resources.LoadAll<WeaponDefinition>(resourcesPath);
        return new List<WeaponDefinition>(assets);
    }

    // Returns up to optionCount random unowned weapons from the provided catalog.
    public List<WeaponDefinition> GetDraftOptions(List<WeaponDefinition> allDefinitions, ISet<string> ownedKeys, int optionCount)
    {
        var result = new List<WeaponDefinition>();
        if (allDefinitions == null || optionCount <= 0)
            return result;

        var candidates = new List<WeaponDefinition>();
        for (int i = 0; i < allDefinitions.Count; i++)
        {
            WeaponDefinition def = allDefinitions[i];
            if (def != null && (ownedKeys == null || !ownedKeys.Contains(def.Key)))
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
