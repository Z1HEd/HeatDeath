using UnityEngine;

[CreateAssetMenu(fileName = "ShipModuleDefinition", menuName = "HeatDeath/Ship Module Definition")]
public class ShipModuleDefinition : ModuleDefinition
{
    [SerializeField] private string key;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private GameObject modulePrefab;

    public string Key => key;
    public string Description => description;
    public GameObject ModulePrefab => modulePrefab;
}
