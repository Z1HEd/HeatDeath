using UnityEngine;
using System.Collections.Generic;
using System;
using MackySoft.SerializeReferenceExtensions;
public class ExplosionAbility : AbilityModule, IHitter
{
    [SerializeField] protected ScalarStat damage = new ScalarStat(StatType.Damage,0f,0f);
    [SerializeField] protected ScalarStat shieldDamageMultiplier = new ScalarStat(StatType.ShieldDamageMultiplier, 1f, -1f);
    [SerializeField] protected ScalarStat hpDamageMultiplier = new ScalarStat(StatType.HPDamageMultiplier, 1f, -1f);
    [SerializeField] protected ScalarStat backstabMultiplier = new ScalarStat(StatType.BackstabMultiplier, 1f, 0f);
    [SerializeField] protected ScalarStat knockback = new ScalarStat(StatType.Knockback, 0f, 0f);
    [SerializeField]
    protected ResourceStat cooldown = new ResourceStat(StatType.Cooldown,1f,0f);
    [SerializeField]
    protected ScalarStat effectMultiplier = new ScalarStat(StatType.EffectMultiplier,1f,0.01f);
    [SerializeField]
    protected ScalarStat range = new ScalarStat(StatType.Range,1f,0.01f);
    [SerializeField]
    private Explosion explosionPrefab;
    [SerializeReference]
    [SubclassSelector]
    protected List<Effect> baseEffects = new();
    [SerializeField]
    private int explosionLayer;
    private List<Effect> appliedEffects = new();
    public override bool IsActivatable => cooldown.CurrentValue == 0f;
    public override bool IsActive => false;
    public override float CooldownFraction => cooldown.Fraction;
    public override float DurationFraction => 0f;
    public float Damage => damage;
    public float ShieldDamageMultiplier => shieldDamageMultiplier;
    public float HPDamageMultiplier => hpDamageMultiplier;
    public float BackstabMultiplier => backstabMultiplier;
    public float KnockbackPower => knockback;

    public List<Effect> GetEffects(){ return EmitEffects(baseEffects,effectMultiplier);}
    public void Update()
    {
        if (cooldown.Tick(Time.deltaTime)) InvokeUpdateActivatable();
        InvokeUpdateCooldown();
    }

    public override void Activate()
    {
        if (!IsActivatable) return;
        cooldown.ResetToMax();
        InvokeUpdateActivatable();

        var instance = Instantiate(explosionPrefab,transform.position,transform.rotation) as Explosion;
        instance.Initialize(this,range,explosionLayer);
    }

    public override void Deactivate(){}

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(damage);
        stats.Add(shieldDamageMultiplier);
        stats.Add(hpDamageMultiplier);
        stats.Add(backstabMultiplier);
        stats.Add(knockback);
        stats.Add(cooldown);
        stats.Add(effectMultiplier);
        stats.Add(range);
    }
}