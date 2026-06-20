using System;
using System.Collections.Generic;
using UnityEngine;

public struct StatModifierAggregate
{
    public float Flat;
    public float Percent;
}

[Serializable]
public abstract class StatBase<T>
{
    private StatType type;

    [NonSerialized] private bool initialized;
    [SerializeField] private T currentValue;

    public event Action<T> CurrentValueChanged;

    public StatType Type => type;
    protected bool IsInitialized => initialized;

    protected StatBase() { }

    protected StatBase(StatType type)
    {
        this.type = type;
    }

    protected T CurrentOr(T fallback)
    {
        return initialized ? currentValue : fallback;
    }

    protected void SetCurrentValue(T value)
    {
        bool changed = !initialized || !EqualityComparer<T>.Default.Equals(currentValue, value);
        currentValue = value;
        initialized = true;

        if (changed)
            CurrentValueChanged?.Invoke(currentValue);
    }

    protected static float CalculateModifiedValue(
        float baseStatValue,
        IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers,
        StatType stat)
    {
        if (modifiers == null || stat == StatType.None)
            return baseStatValue;

        modifiers.TryGetValue(stat, out StatModifierAggregate modifier);
        return (baseStatValue + modifier.Flat) * (1f + (modifier.Percent * 0.01f));
    }
}

[Serializable]
public sealed class ResourceStat : StatBase<float>
{
    [SerializeField] private float baseValue = 1f;
    [SerializeField] private float minValue = 0f;

    [NonSerialized] private float maxValue;

    public ResourceStat() { }

    public ResourceStat(float baseValue, float minValue)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public ResourceStat(StatType type, float baseValue, float minValue) : base(type)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public float BaseValue => baseValue;
    public float CurrentValue => CurrentOr(Mathf.Max(minValue, baseValue));
    public float MaxValue => IsInitialized ? maxValue : Mathf.Max(minValue, baseValue);

    public static implicit operator float(ResourceStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    public void Recalculate(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers, bool preserveCurrentRatio)
    {
        float previousMax = IsInitialized ? Mathf.Max(minValue, maxValue) : 0f;
        float ratio = previousMax > 0f ? CurrentValue / previousMax : 1f;

        maxValue = Mathf.Max(minValue, CalculateModifiedValue(baseValue, modifiers, Type));

        if (!IsInitialized || !preserveCurrentRatio)
        {
            SetCurrentValue(maxValue);
        }
        else
        {
            SetCurrentValue(Mathf.Clamp(maxValue * ratio, 0f, maxValue));
        }
    }

    public void ResetToMax()
    {
        if (!IsInitialized)
            maxValue = Mathf.Max(minValue, baseValue);

        SetCurrentValue(maxValue);
    }

    public void AddCurrent(float value)
    {
        if (!IsInitialized)
            ResetToMax();

        SetCurrentValue(Mathf.Clamp(CurrentValue + value, 0f, MaxValue));
    }

    public float Consume(float value, float multiplier = 1f)
    {
        if (!IsInitialized)
            ResetToMax();

        if (multiplier<0) return value;

        float leftover = 0f;

        if (CurrentValue >= value * multiplier)
        {
            SetCurrentValue(CurrentValue - value*multiplier);
            return leftover;
        }
        leftover = value - CurrentValue/multiplier;
        SetCurrentValue(0f);
        return leftover;
    }
}

[Serializable]
public sealed class ScalarStat : StatBase<float>
{
    [SerializeField] private float baseValue;
    [SerializeField] private float minValue = 0f;

    public ScalarStat() { }

    public ScalarStat(float baseValue, float minValue)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public ScalarStat(StatType type, float baseValue, float minValue) : base(type)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public float BaseValue => baseValue;
    public float CurrentValue => CurrentOr(Mathf.Max(minValue, baseValue));

    public static implicit operator float(ScalarStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    public void Recalculate(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        SetCurrentValue(Mathf.Max(minValue, CalculateModifiedValue(baseValue, modifiers, Type)));
    }

    public void ResetToBase()
    {
        SetCurrentValue(Mathf.Max(minValue, baseValue));
    }
}

[Serializable]
public sealed class BoolStat : StatBase<bool>
{
    [SerializeField] private bool baseValue;

    public BoolStat() { }

    public BoolStat(bool baseValue)
    {
        this.baseValue = baseValue;
    }

    public BoolStat(StatType type, bool baseValue) : base(type)
    {
        this.baseValue = baseValue;
    }

    public bool BaseValue => baseValue;
    public bool CurrentValue => CurrentOr(baseValue);

    public static implicit operator bool(BoolStat stat)
    {
        return stat != null && stat.CurrentValue;
    }

    public void Recalculate(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        if (modifiers == null || Type == StatType.None)
        {
            SetCurrentValue(baseValue);
            return;
        }

        modifiers.TryGetValue(Type, out StatModifierAggregate modifier);
        SetCurrentValue(modifier.Flat > 0f ? true : (modifier.Flat < 0f ? false : baseValue));
    }

    public void ResetToBase()
    {
        SetCurrentValue(baseValue);
    }
}