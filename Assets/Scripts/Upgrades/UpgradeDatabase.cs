using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "HeatDeath/Upgrades/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

    [Header("Rarity Weights")]
    [SerializeField] private int commonWeight = 70;
    [SerializeField] private int rareWeight = 20;
    [SerializeField] private int epicWeight = 8;
    [SerializeField] private int legendaryWeight = 2;

    public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

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
