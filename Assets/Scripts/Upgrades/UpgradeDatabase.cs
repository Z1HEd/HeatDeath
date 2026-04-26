using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "HeatDeath/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("Rarity Weights")]
    [SerializeField] private int commonWeight = 70;
    [SerializeField] private int rareWeight = 20;
    [SerializeField] private int epicWeight = 8;
    [SerializeField] private int legendaryWeight = 2;

    public int GetRarityWeight(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common:    return commonWeight;
            case UpgradeRarity.Rare:      return rareWeight;
            case UpgradeRarity.Epic:      return epicWeight;
            case UpgradeRarity.Legendary: return legendaryWeight;
            default:                      return 0;
        }
    }
}
