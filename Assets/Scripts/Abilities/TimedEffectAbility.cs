using UnityEngine;
using System.Collections.Generic;
using System;
public class TimedEffectAbility : AbilityModule
{
    [SerializeField]
    protected ResourceStat cooldown = new ResourceStat(StatType.Cooldown,1f,0f);
    [SerializeField]
    protected ResourceStat duration = new ResourceStat(StatType.Duration,1f,0f);
    [SerializeField]
    protected ScalarStat effectMultiplier = new ScalarStat(StatType.EffectMultiplier,1f,0.01f);
    [SerializeReference]
    [SubclassSelector]
    protected List<Effect> baseEffects = new();
    private bool isActive = false;
    public override bool IsActivatable => !isActive && cooldown.CurrentValue == 0f;
    public override bool IsActive => isActive;
    public override float CooldownFraction => cooldown.Fraction;
    public override float DurationFraction => duration.Fraction;
    public void Update()
    {
        if (isActive)
        {
            duration.Consume(Time.deltaTime);
            if (duration <=0) Deactivate();
            InvokeUpdateDuration();
        }
        else{
            if (cooldown.Tick(Time.deltaTime)) InvokeUpdateActivatable();
            InvokeUpdateCooldown();
        }
    }

    public override void Activate()
    {
        if (!IsActivatable) return;
        isActive = true;
        duration.ResetToMax();
        ship.effectManager.Apply(this, EmitEffects(baseEffects, effectMultiplier));
        InvokeUpdateActivatable();
    }

    public override void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        cooldown.ResetToMax();
        InvokeUpdateActivatable();
        ship.effectManager.Remove(this);
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(cooldown);
        stats.Add(duration);
        stats.Add(effectMultiplier);
    }
}