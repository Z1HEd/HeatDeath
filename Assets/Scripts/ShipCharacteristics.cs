using System;
using UnityEngine;

[Serializable]
public struct ShipCharacteristics
{
    [SerializeField] private int maxHealth;
    [SerializeField] private int maxShields;

    public int MaxHealth => maxHealth;
    public int MaxShields => maxShields;

    public ShipCharacteristics(int maxHealth, int maxShields)
    {
        this.maxHealth = maxHealth;
        this.maxShields = maxShields;
    }
}