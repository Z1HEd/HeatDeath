using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
	None = 0,
	Health = 1,
	Shields = 2,
	ShieldRegen = 3,
	Thrust = 4,
	MaxSpeed = 5,
	Damage = 6,
	Knockback = 7,
	Cooldown = 8,
	ProjectileSpeed = 9,
	BackstabMultiplier = 10,
	ProjectileKnockback = 11,
	ProjectileCount = 12,
	Range = 13,
	ProjectileSpread = 14,
	CanAim = 15,
	ProjectilePiercing = 16,
	Lifetime = 17,
	ShieldDamageMultiplier = 18,
	HPDamageMultiplier = 19,
	ExplosionRange = 20,
	ProjectileRange = 21,
	TimeScale = 22,
	HPRegen = 23,
    Duration = 24,
    EffectMultiplier = 25,
    Invincibility = 26,
    XPGain = 27,
    ShieldRegenDelay = 28,
}

[Serializable]
public struct StatModifier
{
    public float Flat;
    public float Percent;
    public StatModifier(float flat, float percent)
    {
        Flat = flat;
        Percent = percent;
    }

    public static StatModifier operator*(StatModifier left, float right) 
    {
        return new StatModifier(left.Flat * right, left.Percent * right);
    }

    // Flats add up. Percents multiply as factors, so a -100% stays exactly -100% whatever else applies.
    // A percent below -100% (say, scaled up by a multiplier) counts as -100%: two negative factors
    // would otherwise multiply into a positive one.
    public static StatModifier Combine(StatModifier a, StatModifier b)
    {
        float factor = Mathf.Max(0f, 1f + a.Percent / 100f) * Mathf.Max(0f, 1f + b.Percent / 100f);
        return new StatModifier(a.Flat + b.Flat, (factor - 1f) * 100f);
    }
}

public interface IStat
{
    void Initialize();
    void Recalculate(IReadOnlyDictionary<StatType, StatModifier> modifiers);
}

[Serializable]
public abstract class StatBase<T> : IStat
{
    private StatType type;

    [SerializeField, ReadOnly] protected T currentValue;

    public StatType Type => type;
    public T CurrentValue => currentValue;

    protected StatBase() { }

    protected StatBase(StatType type)
    {
        this.type = type;
    }

    public abstract void Initialize();
    public abstract void Recalculate(IReadOnlyDictionary<StatType, StatModifier> modifiers);

    protected StatModifier GetModifier(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        if (type == StatType.None)
            return default;

        modifiers.TryGetValue(type, out StatModifier modifier);
        return modifier;
    }
}

[Serializable]
public abstract class FloatStat : StatBase<float>
{
    [SerializeField] protected float baseValue;
    [SerializeField] protected float minValue;

    protected FloatStat() { }

    protected FloatStat(StatType type, float baseValue, float minValue) : base(type)
    {
        this.baseValue = baseValue;
        this.minValue = minValue;
    }

    public float BaseValue => baseValue;
    public float MinValue => minValue;

    public static implicit operator float(FloatStat stat)
    {
        return stat != null ? stat.CurrentValue : 0f;
    }

    protected float CalculateModifiedValue(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        StatModifier modifier = GetModifier(modifiers);

        // Divide rather than multiply by 0.01f, which is inexact: -100% has to give exactly 0.
        return Mathf.Max(minValue, (baseValue + modifier.Flat) * (1f + modifier.Percent / 100f));
    }
}

// minValue is the floor for MaxValue only. CurrentValue always stays within 0..MaxValue.
[Serializable]
public sealed class ResourceStat : FloatStat
{
    [NonSerialized] private float maxValue;

    public ResourceStat() { baseValue = 1f; }

    public ResourceStat(StatType type, float baseValue, float minValue) : base(type, baseValue, minValue) { }

    public float MaxValue => maxValue;
    // Current/max, or 0 when the max is 0 (e.g. cut by -100%), so callers never divide by 0.
    public float Fraction => maxValue > 0f ? currentValue / maxValue : 0f;

    public override void Initialize()
    {
        maxValue = Mathf.Max(minValue, baseValue);
        currentValue = maxValue;
    }

    // Keeps the current/max ratio, so a ship at half health stays at half when its max changes.
    public override void Recalculate(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        float ratio = maxValue > 0f ? currentValue / maxValue : 0f;

        maxValue = CalculateModifiedValue(modifiers);
        currentValue = Mathf.Clamp(maxValue * ratio, 0f, maxValue);
    }

    public void ResetToMax()
    {
        currentValue = maxValue;
    }

    public void AddCurrent(float value)
    {
        currentValue = Mathf.Clamp(currentValue + value, 0f, maxValue);
    }

    // Returns true only on the call that reaches 0.
    public bool Tick(float delta)
    {
        if (currentValue <= 0f)
            return false;

        Consume(delta);
        return currentValue <= 0f;
    }

    public float Consume(float value, float multiplier = 1f)
    {
        if (multiplier<=0) return value;

        if (currentValue >= value * multiplier)
        {
            currentValue -= value * multiplier;
            return 0f;
        }

        float leftover = value - currentValue / multiplier;
        currentValue = 0f;
        return leftover;
    }
}

[Serializable]
public sealed class ScalarStat : FloatStat
{
    public ScalarStat() { }

    public ScalarStat(StatType type, float baseValue, float minValue) : base(type, baseValue, minValue) { }

    public override void Initialize()
    {
        currentValue = Mathf.Max(minValue, baseValue);
    }

    public override void Recalculate(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        currentValue = CalculateModifiedValue(modifiers);
    }
}

[Serializable]
public sealed class BoolStat : StatBase<bool>
{
    [SerializeField] private bool baseValue;

    public BoolStat() { }

    public BoolStat(StatType type, bool baseValue) : base(type)
    {
        this.baseValue = baseValue;
    }

    public bool BaseValue => baseValue;

    public static implicit operator bool(BoolStat stat)
    {
        return stat != null && stat.CurrentValue;
    }

    public override void Initialize()
    {
        currentValue = baseValue;
    }

    public override void Recalculate(IReadOnlyDictionary<StatType,StatModifier> modifiers)
    {
        StatModifier modifier = GetModifier(modifiers);
        currentValue = modifier.Flat > 0f ? true : (modifier.Flat < 0f ? false : baseValue);
    }
}