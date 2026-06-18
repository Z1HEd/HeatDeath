using System.Collections.Generic;
using UnityEngine;

public class ShipDraftService
{
    private readonly List<ShipDefinition> definitions;
    private readonly System.Random random;

    public ShipDraftService(int? seed = null)
    {
        definitions = new List<ShipDefinition>(Resources.LoadAll<ShipDefinition>("Ships"));
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public List<ShipDefinition> GetDraftOptions(int optionCount)
    {
        var result = new List<ShipDefinition>();
        if (optionCount <= 0)
            return result;

        var candidates = new List<ShipDefinition>();
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null)
                candidates.Add(definitions[i]);
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