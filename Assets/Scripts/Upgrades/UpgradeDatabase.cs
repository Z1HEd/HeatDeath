using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "HeatDeath/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

    [Header("Rarity Weights")]
    [SerializeField] private int commonWeight = 70;
    [SerializeField] private int rareWeight = 20;
    [SerializeField] private int epicWeight = 8;
    [SerializeField] private int legendaryWeight = 2;

    public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateAndLogWarnings();
    }
#endif

    public void ValidateAndLogWarnings()
    {
        var seenKeys = new HashSet<string>();

        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeDefinition upgrade = upgrades[i];
            if (upgrade == null)
                continue;

            if (string.IsNullOrWhiteSpace(upgrade.Key))
            {
                Debug.LogWarning($"Upgrade key not set: {upgrade.name}", upgrade);
            }
            else if (!seenKeys.Add(upgrade.Key))
            {
                Debug.LogWarning($"Duplicate upgrade key in database: {upgrade.Key}", upgrade);
            }

            IReadOnlyList<StatModifier> modifiers = upgrade.Modifiers;
            for (int j = 0; j < modifiers.Count; j++)
            {
                StatModifier modifier = modifiers[j];
                bool invalidType = modifier.stat == StatType.None;
                bool missingTagsOnGeneral = upgrade.IsGeneral && !modifier.HasTargetTags;
                if (invalidType || missingTagsOnGeneral)
                {
                    Debug.LogWarning($"Invalid modifier found in upgrade: {upgrade.name}", upgrade);
                    break;
                }
            }
        }
    }

    public int GetRarityWeight(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common:
                return commonWeight;
            case UpgradeRarity.Rare:
                return rareWeight;
            case UpgradeRarity.Epic:
                return epicWeight;
            case UpgradeRarity.Legendary:
                return legendaryWeight;
            default:
                return 0;
        }
    }
}
