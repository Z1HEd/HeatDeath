using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeStatDefinition", menuName = "HeatDeath/Upgrades/Upgrade Stat")]
public class UpgradeStatDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string key;

    public string DisplayName => displayName;
    public string Key => key;
}
