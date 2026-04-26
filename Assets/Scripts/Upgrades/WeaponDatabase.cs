using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "HeatDeath/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [SerializeField] private List<WeaponDefinition> weapons = new List<WeaponDefinition>();

    public IReadOnlyList<WeaponDefinition> Weapons => weapons;
}
