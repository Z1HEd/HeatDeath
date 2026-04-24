using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "HeatDeath/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [SerializeField] private string key;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private GameObject weaponPrefab;

    public string Key => key;
    public string DisplayName => displayName;
    public string Description => description;
    public GameObject WeaponPrefab => weaponPrefab;
}
