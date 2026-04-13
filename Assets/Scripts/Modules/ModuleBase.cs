using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public struct StatModifierAggregate
{
    public float Flat;
    public float Percent;
}

[System.Serializable]
public class ResourceStat
{
    [FormerlySerializedAs("definition")]
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
    [FormerlySerializedAs("definition")]
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

[RequireComponent(typeof(Ship))]
public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    protected UpgradeManager upgradeManager;
    public ModuleDefinition ModuleDefinition => moduleDefinition;

    protected virtual void Start()
    {
        ship = GetComponent<Ship>();
        upgradeManager = GetComponent<UpgradeManager>();
        ship.AddModule(this);
        ResetModifiers();
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
    }

    protected IReadOnlyDictionary<StatType, StatModifierAggregate> GetCurrentModifiers()
    {
        var result = new Dictionary<StatType, StatModifierAggregate>();
        if (upgradeManager == null || moduleDefinition == null)
            return result;

        IReadOnlyList<ActiveStatModifier> effects = upgradeManager.ActiveEffects;
        for (int i = 0; i < effects.Count; i++)
        {
            ActiveStatModifier effect = effects[i];
            if (!effect.AppliesToModule(moduleDefinition))
                continue;

            AddModifier(result, effect);
        }

        return result;
    }

    public virtual void UpdateModifiers()
    {
        ResetModifiers();
        ApplyModifiers(GetCurrentModifiers());
    }

    private static void AddModifier(Dictionary<StatType, StatModifierAggregate> map, ActiveStatModifier modifierData)
    {
        StatModifier data = modifierData.Modifier;
        if (!map.TryGetValue(data.stat, out StatModifierAggregate modifier))
            modifier = default;

        if (data.operation == ModifierOperation.AddPercent)
            modifier.Percent += data.value;
        else
            modifier.Flat += data.value;

        map[data.stat] = modifier;
    }

    protected abstract void ResetModifiers();

    protected abstract void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers);
}
