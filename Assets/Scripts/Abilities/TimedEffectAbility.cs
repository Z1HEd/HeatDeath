using UnityEngine;
using System.Collections.Generic;
using System;
using MackySoft.SerializeReferenceExtensions;
public class TimedEffectAbility : AbilityModule
{
    [SerializeField]
    protected ResourceStat cooldown = new ResourceStat(StatType.Cooldown,1f,0f);
    [SerializeField]
    protected ResourceStat duration = new ResourceStat(StatType.AbilityDuration,1f,0f);
    [SerializeField]
    protected ScalarStat effectMultiplier = new ScalarStat(StatType.EffectMuliplier,1f,0.01f);
    [SerializeReference]
    [SubclassSelector]
    protected List<Effect> baseEffects = new();
    private List<Effect> appliedEffects = new List<Effect>();
    private bool isActive = false;
    public override bool IsActivatable => !isActive && cooldown.CurrentValue == 0f;
    public override bool IsActive => isActive;
    public override float CooldownFraction => cooldown/cooldown.MaxValue;
    public override float DurationFraction => duration/duration.MaxValue;
    public void Update()
    {
        if (isActive)
        {
            duration.Consume(Time.deltaTime);
            if (duration <=0) Deactivate();
            InvokeUpdateDuration();
        }
        else{
            if (cooldown.Consume(Time.deltaTime)>0f) InvokeUpdateActivatable();
            InvokeUpdateCooldown();
        }
    }

    public override void Activate()
    {
        if (!IsActivatable) return;
        isActive = true;
        duration.ResetToMax();
        foreach (var effect in baseEffects)
        {
            Effect appliedEffect = effect.Stacked(effectMultiplier);
            appliedEffects.Add(appliedEffect);
            ship.effectManager.AddEffect(appliedEffect);
        }
        InvokeUpdateActivatable();
    }

    public override void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        InvokeUpdateActivatable();
        cooldown.ResetToMax();
        foreach (var effect in appliedEffects)
            ship.effectManager.RemoveEffect(effect);
    }

    protected override void ApplyModifiers()
    {
        base.ApplyModifiers();

        cooldown.Recalculate(currentModifiers,true);
        duration.Recalculate(currentModifiers,true);
        effectMultiplier.Recalculate(currentModifiers);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();

        effectMultiplier.ResetToBase();
    }
}