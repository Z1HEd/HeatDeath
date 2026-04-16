using UnityEngine;
using System.Collections.Generic;

public struct StatModifierAggregate
{
    public float Flat;
    public float Percent;
}

[System.Serializable]
public class ResourceStat
{
    [SerializeField] private StatType type;
    [SerializeField] private float baseValue = 1f;
    [SerializeField] private float minValue = 0f;

    private float currentValue;
    private float maxValue;
    private bool initialized;

    public ResourceStat()
    {
    }

    public ResourceStat(float baseValue, float minValue)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }
    public ResourceStat(StatType type, float baseValue, float minValue)
    {
        this.type = type;
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public StatType Type => type;
    public float BaseValue => baseValue;
    public float CurrentValue => initialized ? currentValue : Mathf.Max(minValue, baseValue);
    public float MaxValue => initialized ? maxValue : Mathf.Max(minValue, baseValue);

    public static implicit operator float(ResourceStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    public void Recalculate(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers, bool preserveCurrentRatio)
    {
        float previousMax = initialized ? Mathf.Max(minValue, maxValue) : 0f;
        float ratio = previousMax > 0f ? currentValue / previousMax : 1f;

        maxValue = Mathf.Max(minValue, CalculateModifiedValue(baseValue, modifiers, type));

        if (!initialized || !preserveCurrentRatio)
            currentValue = maxValue;
        else
            currentValue = Mathf.Clamp(maxValue * ratio, 0f, maxValue);

        initialized = true;
    }

    public void ResetToMax()
    {
        if (!initialized)
            maxValue = Mathf.Max(minValue, baseValue);

        currentValue = maxValue;
        initialized = true;
    }

    public void AddCurrent(float value)
    {
        if (!initialized)
            ResetToMax();

        currentValue = Mathf.Clamp(currentValue + value, 0f, maxValue);
    }

    public float Consume(float value)
    {
        if (!initialized)
            ResetToMax();

        float amount = Mathf.Max(0f, value);
        if (currentValue >= amount)
        {
            currentValue -= amount;
            return 0f;
        }

        float leftover = amount - currentValue;
        currentValue = 0f;
        return leftover;
    }

    private static float CalculateModifiedValue(float baseStatValue, IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers, StatType stat)
    {
        if (modifiers == null || stat == StatType.None)
            return baseStatValue;

        modifiers.TryGetValue(stat, out StatModifierAggregate modifier);
        return (baseStatValue + modifier.Flat) * (1f + (modifier.Percent * 0.01f));
    }
}

[System.Serializable]
public class ScalarStat
{
    [SerializeField] private StatType type;
    [SerializeField] private float baseValue;
    [SerializeField] private float minValue = 0f;

    private float currentValue;
    private bool initialized;

    public ScalarStat()
    {
    }

    public ScalarStat(float baseValue, float minValue)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }
    public ScalarStat(StatType type, float baseValue, float minValue)
    {
        this.type = type;
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public StatType Type => type;
    public float BaseValue => baseValue;
    public float CurrentValue => initialized ? currentValue : Mathf.Max(minValue, baseValue);

    public static implicit operator float(ScalarStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    public void Recalculate(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        currentValue = Mathf.Max(minValue, CalculateModifiedValue(baseValue, modifiers, type));
        initialized = true;
    }

    public void ResetToBase()
    {
        currentValue = Mathf.Max(minValue, baseValue);
        initialized = true;
    }

    private static float CalculateModifiedValue(float baseStatValue, IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers, StatType stat)
    {
        if (modifiers == null || stat == StatType.None)
            return baseStatValue;

        modifiers.TryGetValue(stat, out StatModifierAggregate modifier);
        return (baseStatValue + modifier.Flat) * (1f + (modifier.Percent * 0.01f));
    }
}

[System.Serializable]
public class BoolStat
{
    [SerializeField] private StatType type;
    [SerializeField] private bool baseValue;

    private bool currentValue;
    private bool initialized;

    public BoolStat()
    {
    }

    public BoolStat(bool baseValue)
    {
        this.baseValue = baseValue;
    }
    public BoolStat(StatType type, bool baseValue)
    {
        this.type = type;
        this.baseValue = baseValue;
    }

    public StatType Type => type;
    public bool BaseValue => baseValue;
    public bool CurrentValue => initialized ? currentValue : baseValue;

    public static implicit operator bool(BoolStat stat)
    {
        return stat != null ? stat.CurrentValue : false;
    }

    public void Recalculate(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        if (modifiers == null || type == StatType.None)
        {
            currentValue = baseValue;
            initialized = true;
            return;
        }

        modifiers.TryGetValue(type, out StatModifierAggregate modifier);
        currentValue = modifier.Flat > 0f ? true : (modifier.Flat < 0f ? false : baseValue);
        initialized = true;
    }
}
