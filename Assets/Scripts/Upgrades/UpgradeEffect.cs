using System;
using UnityEngine;

public enum UpgradeEffectOperation
{
    AddFlat = 0,
    AddPercent = 1
}

[Serializable]
public struct UpgradeEffect
{
    [SerializeField] public UpgradeStatDefinition stat;
    [SerializeField] public UpgradeEffectOperation operation;
    [SerializeField] public float value;
}
