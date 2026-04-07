using System;
using UnityEngine;

[Serializable]
public struct ShipCharacteristics
{
    [SerializeField] public int maxHealth;
    [SerializeField] public int maxShields;
    [SerializeField] public float shieldRegen;

    public ShipCharacteristics(int maxHealth, int maxShields, float shieldRegen)
    {
        this.maxHealth = maxHealth;
        this.maxShields = maxShields;
        this.shieldRegen = shieldRegen;
    }
}