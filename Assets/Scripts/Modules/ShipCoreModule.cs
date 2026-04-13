using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Ship))]
public class ShipCoreModule : ModuleBase
{
    [Header("Base Core Stats")]
    [SerializeField] private ResourceStat health = new ResourceStat(100f, 1f);
    [SerializeField] private ResourceStat shields = new ResourceStat(100f, 0f);
    [SerializeField] private ScalarStat shieldRegen = new ScalarStat(2f, 0f);

    public event Action OnHPShieldsChanged;

    public float CurrentHealth => health.CurrentValue;
    public float CurrentShields => shields.CurrentValue;
    public int CurrentMaxHealth => Mathf.RoundToInt(health.MaxValue);
    public int CurrentMaxShields => Mathf.RoundToInt(shields.MaxValue);
    public float CurrentShieldRegen => shieldRegen.CurrentValue;

    public void Initialize()
    {
        UpdateModifiers();
        ResetModifiers();
    }

    private void Update()
    {
        if (shields.CurrentValue >= shields.MaxValue)
            return;

        shields.AddCurrent(CurrentShieldRegen * Time.deltaTime);
        OnHPShieldsChanged?.Invoke();
    }

    public bool ApplyDamage(float incomingDamage)
    {
        float damage = shields.Consume(incomingDamage);
        if (damage > 0f)
            health.Consume(damage);

        OnHPShieldsChanged?.Invoke();
        return health.CurrentValue <= 0f;
    }

    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        health.Recalculate(modifiers, true);
        shields.Recalculate(modifiers, true);
        shieldRegen.Recalculate(modifiers);

        OnHPShieldsChanged?.Invoke();
    }

    protected override void ResetModifiers()
    {
        health.ResetToMax();
        shields.ResetToMax();
        OnHPShieldsChanged?.Invoke();
    }
}
