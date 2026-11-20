using UnityEngine;
using System.Collections.Generic;
using System;
public class ExplosionAbility : AbilityModule
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
    [Tooltip("The blast's diameter.")]
    [SerializeField]
    protected ScalarStat range = new ScalarStat(StatType.Range,1f,0.01f);
    [SerializeField]
    private Explosion explosionPrefab;
    [SerializeReference]
    [SubclassSelector]
    protected List<Effect> baseEffects = new();
    [SerializeField]
    private int explosionLayer;
    public override bool IsActivatable => cooldown.CurrentValue == 0f;
    public override float CooldownLeft => cooldown.Fraction;

    private HitEffects GetEffects() => EmitHitEffects(baseEffects, effectMultiplier);

    // The blast's damage and types. Abilities have no damage bonuses and no weapon.
    private HitPayload CreatePayload() => new HitPayload(damage, shieldDamageMultiplier, hpDamageMultiplier,
        backstabMultiplier, knockback, default, ModuleDefinition, ship, null);
    private void Update()
    {
        if (cooldown.Tick(Time.deltaTime)) RaiseActivatableChanged();
        RaiseCooldownChanged();
    }

    public override void Activate()
    {
        if (!IsActivatable) return;
        cooldown.ResetToMax();
        RaiseActivatableChanged();

        Explosion blast = Instantiate(explosionPrefab, transform.position, transform.rotation);
        blast.Initialize(CreatePayload(), transform.position, GetEffects(), range, explosionLayer);
    }

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