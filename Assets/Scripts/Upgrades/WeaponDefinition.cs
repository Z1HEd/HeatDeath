using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "HeatDeath/Weapon Definition")]
public class WeaponDefinition : ModuleDefinition
{
    [SerializeField] private string key;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private GameObject weaponPrefab;

    public string Key => key;
    public string Description => description;
    public GameObject WeaponPrefab => weaponPrefab;
}
