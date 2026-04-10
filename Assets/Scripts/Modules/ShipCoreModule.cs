using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Ship))]
public class ShipCoreModule : ModuleBase
{
    private const string MaxHealthKey = "stat.core.max_health";
    private const string MaxShieldsKey = "stat.core.max_shields";
    private const string ShieldRegenKey = "stat.core.shield_regen";

    [Header("Base Core Stats")]
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private int baseMaxShields = 100;
    [SerializeField] private float baseShieldRegen = 2f;

    public event Action OnHPShieldsChanged;

    private int currentMaxHealth;
    private int currentMaxShields;
    public float CurrentShieldRegen { get; private set; }
    private float currentHealth;
    private float currentShields;

    public float CurrentHealth => currentHealth;
    public float CurrentShields => currentShields;
    public int CurrentMaxHealth => currentMaxHealth;
    public int CurrentMaxShields => currentMaxShields;

    public void Initialize()
    {
        Recalculate(true);
    }

    private void Update()
    {
        if (currentShields >= currentMaxShields)
            return;

        currentShields = Mathf.Min(currentShields + CurrentShieldRegen * Time.deltaTime, currentMaxShields);
        OnHPShieldsChanged?.Invoke();
    }

    public bool ApplyDamage(float incomingDamage)
    {
        float damage = Mathf.Max(0f, incomingDamage);

        if (currentShields < damage)
        {
            damage -= currentShields;
            currentShields = 0f;
        }
        else
        {
            currentShields -= damage;
            damage = 0f;
        }

        if (damage > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - damage);
        }

        OnHPShieldsChanged?.Invoke();
        return currentHealth <= 0f;
    }

    public override void Recalculate()
    {
        Recalculate(false);
    }

    private void Recalculate(bool resetCurrentValues)
    {
        UpgradeManager upgradeManager = ship != null ? ship.GetComponent<UpgradeManager>() : GetComponent<UpgradeManager>();
        Dictionary<string, StatModifier> modifiers = BuildStatModifiers(upgradeManager);

        float nextMaxHealth = ResolveValue(baseMaxHealth, modifiers, MaxHealthKey);
        float nextMaxShields = ResolveValue(baseMaxShields, modifiers, MaxShieldsKey);
        float nextShieldRegen = ResolveValue(baseShieldRegen, modifiers, ShieldRegenKey);

        float previousMaxHealth = Mathf.Max(1f, currentMaxHealth);
        float previousMaxShields = Mathf.Max(1f, currentMaxShields);
        float healthRatio = currentHealth / previousMaxHealth;
        float shieldsRatio = currentShields / previousMaxShields;

        currentMaxHealth = Mathf.Max(1, Mathf.RoundToInt(nextMaxHealth));
        currentMaxShields = Mathf.Max(0, Mathf.RoundToInt(nextMaxShields));
        CurrentShieldRegen = Mathf.Max(0f, nextShieldRegen);

        if (resetCurrentValues)
        {
            currentHealth = currentMaxHealth;
            currentShields = currentMaxShields;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentMaxHealth * healthRatio, 0f, currentMaxHealth);
            currentShields = Mathf.Clamp(currentMaxShields * shieldsRatio, 0f, currentMaxShields);
        }

        OnHPShieldsChanged?.Invoke();
    }

    private float ResolveValue(float baseValue, Dictionary<string, StatModifier> modifiers, string statKey)
    {
        if (!modifiers.TryGetValue(statKey, out StatModifier modifier))
            return baseValue;

        return (baseValue + modifier.Flat) * (1f + modifier.Percent);
    }
}
