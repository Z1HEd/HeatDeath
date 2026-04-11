using UnityEngine;
using System.Collections.Generic;

public struct StatModifier
{
    public float Flat;
    public float Percent;
}

[System.Serializable]
public class ResourceStat
{
    [SerializeField] private StatDefinition definition;
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

    public StatDefinition Definition => definition;
    public float BaseValue => baseValue;
    public float CurrentValue => initialized ? currentValue : Mathf.Max(minValue, baseValue);
    public float MaxValue => initialized ? maxValue : Mathf.Max(minValue, baseValue);

    public static implicit operator float(ResourceStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    public void Recalculate(IReadOnlyDictionary<StatDefinition, StatModifier> modifiers, bool preserveCurrentRatio)
    {
        float previousMax = initialized ? Mathf.Max(minValue, maxValue) : 0f;
        float ratio = previousMax > 0f ? currentValue / previousMax : 1f;

        maxValue = Mathf.Max(minValue, CalculateModifiedValue(baseValue, modifiers, definition));

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

    private static float CalculateModifiedValue(float baseStatValue, IReadOnlyDictionary<StatDefinition, StatModifier> modifiers, StatDefinition stat)
    {
        if (modifiers == null || stat == null)
            return baseStatValue;

        modifiers.TryGetValue(stat, out StatModifier modifier);
        return (baseStatValue + modifier.Flat) * (1f + (modifier.Percent * 0.01f));
    }
}

[System.Serializable]
public class ScalarStat
{
    [SerializeField] private StatDefinition definition;
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

    public StatDefinition Definition => definition;
    public float BaseValue => baseValue;
    public float CurrentValue => initialized ? currentValue : Mathf.Max(minValue, baseValue);

    public static implicit operator float(ScalarStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    public void Recalculate(IReadOnlyDictionary<StatDefinition, StatModifier> modifiers)
    {
        currentValue = Mathf.Max(minValue, CalculateModifiedValue(baseValue, modifiers, definition));
        initialized = true;
    }

    public void ResetToBase()
    {
        currentValue = Mathf.Max(minValue, baseValue);
        initialized = true;
    }

    private static float CalculateModifiedValue(float baseStatValue, IReadOnlyDictionary<StatDefinition, StatModifier> modifiers, StatDefinition stat)
    {
        if (modifiers == null || stat == null)
            return baseStatValue;

        modifiers.TryGetValue(stat, out StatModifier modifier);
        return (baseStatValue + modifier.Flat) * (1f + (modifier.Percent * 0.01f));
    }
}

[RequireComponent(typeof(Ship))]
public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    protected UpgradeManager upgradeManager;
    protected IReadOnlyDictionary<StatDefinition, StatModifier> CurrentModifiers =>
        upgradeManager != null ? upgradeManager.StatModifiers : null;
    public ModuleDefinition ModuleDefinition => moduleDefinition;

    protected virtual void Start()
    {
        ship = GetComponent<Ship>();
        upgradeManager = GetComponent<UpgradeManager>();
        ship.AddModule(this);
        ResetValues();
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
    }

    public abstract void Recalculate();

    protected abstract void ResetValues();
}
