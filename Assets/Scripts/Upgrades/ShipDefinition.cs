using UnityEngine;

[CreateAssetMenu(menuName = "HeatDeath/Ship Definition")]
public class ShipDefinition : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    public GameObject ShipPrefab;
}