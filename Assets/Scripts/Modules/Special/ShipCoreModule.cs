using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class ShipCoreModule : ModuleBase, IHitter
{
    [Header("Base Core Stats")]
    [SerializeField] private ResourceStat health = new ResourceStat(StatType.Health, 100f, 1f);
    [SerializeField] private ResourceStat shields = new ResourceStat(StatType.Shields, 100f, 0f);
    [SerializeField] private ScalarStat shieldRegen = new ScalarStat(StatType.ShieldRegen, 2f, 0f);
    [SerializeField] private ScalarStat hpRegen = new ScalarStat(StatType.HPRegen, 0f, 0f);
    [SerializeField] private BoolStat invincibility = new BoolStat(StatType.Invincibility,false);

    [Header("Ramming")]
    [SerializeField] private ScalarStat damage = new ScalarStat(StatType.Damage, 10f, 0f);
    [SerializeField] private ScalarStat knockbackPower = new ScalarStat(StatType.Knockback, 1f, 0f);
    [SerializeField] private ScalarStat backstabMultiplier = new ScalarStat(StatType.BackstabMultiplier, 1f, 0f);
    [SerializeReference, SubclassSelector] private List<Effect> appliesEffects = new();

    protected SpriteRenderer shieldRenderer;

    public event Action OnHPShieldsChanged;

    public float CurrentHealth => health.CurrentValue;
    public float CurrentShields => shields.CurrentValue;
    public int CurrentMaxHealth => Mathf.RoundToInt(health.MaxValue);
    public int CurrentMaxShields => Mathf.RoundToInt(shields.MaxValue);
    public float HealthFraction => health.Fraction;
    public float ShieldFraction => shields.Fraction;
    public float CurrentShieldRegen => shieldRegen.CurrentValue;
    public float CurrentHPRegen => hpRegen.CurrentValue;
    public bool IsFull => health.CurrentValue >= health.MaxValue && shields.CurrentValue >= shields.MaxValue;
    public float Damage => damage;
    public float KnockbackPower => knockbackPower;
    public float ShieldDamageMultiplier => 1f;
    public float HPDamageMultiplier => 1f;
    public float BackstabMultiplier => backstabMultiplier;
    public List<Effect> GetEffects() {return EmitEffects(appliesEffects);}

    protected override void Awake()
    {
        base.Awake();
        shieldRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (shields.CurrentValue < shields.MaxValue){
            shields.AddCurrent(CurrentShieldRegen * Time.deltaTime);
            OnHPShieldsChanged?.Invoke();
        }

        if (health.CurrentValue < health.MaxValue){
            health.AddCurrent(CurrentHPRegen * Time.deltaTime);
            OnHPShieldsChanged?.Invoke();
        }
        
    }

    public bool ApplyDamage(IHitter hitter)
    {
        float damage = hitter.Damage;
        if (invincibility) return false;
        if (hitter.transform.position.y>transform.position.y) 
            damage *= hitter.BackstabMultiplier;
        
        damage = shields.Consume(damage,hitter.ShieldDamageMultiplier);
        if (damage > 0f)
            health.Consume(damage,hitter.HPDamageMultiplier);
        else
        {
            StopAllCoroutines();
            StartCoroutine(FlashShield());
        }

        OnHPShieldsChanged?.Invoke();
        return damage>0;
    }
    
    protected IEnumerator FlashShield()
    {
        const float SHIELD_FLASH_DURATION = 0.5f;
        for (float a = 0.65f; a>0; a-=0.01f)
        {
            shieldRenderer.color = new Color(1,1,1,a);
            yield return new WaitForSeconds(0.01f*SHIELD_FLASH_DURATION);
        }
        
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(health);
        stats.Add(shields);
        stats.Add(shieldRegen);
        stats.Add(hpRegen);
        stats.Add(invincibility);
        stats.Add(damage);
        stats.Add(knockbackPower);
        stats.Add(backstabMultiplier);
    }

    protected override void OnStatsChanged()
    {
        OnHPShieldsChanged?.Invoke();
    }
}
