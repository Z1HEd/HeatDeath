using System.Collections.Generic;
using UnityEngine;

// A drafted module with one role, such as defense, mobility, or economy. It isn't a weapon, though a
// feature may deal incidental damage (reflection, a burst on hit). Its features decide what it does;
// it forwards its lifecycle and stats to them. EffectMultiplier scales what they grant, and upgrades
// on the module's own tag raise it.
public class ShipModule : ModuleBase
{
    [SerializeField] private ScalarStat effectMultiplier = new(StatType.EffectMultiplier, 1f, 0f);
    [Tooltip("What the module does. Its effects must not target this module's own tag: they would feed its own multiplier.")]
    [SerializeReference, SubclassSelector] private List<ShipModuleFeature> features = new();

    // NaN, so the first recalculation counts as a change.
    private float lastMultiplier = float.NaN;

    public Ship Ship => ship;

    // Copies at the module's EffectMultiplier, with durations scaled by its Duration modifier.
    public List<Effect> Emit(IReadOnlyList<Effect> templates) => EmitEffects(templates, effectMultiplier);

    protected override void Start()
    {
        base.Start();
        foreach (var feature in features)
            feature?.OnStart(this);
    }

    private void Update()
    {
        foreach (var feature in features)
            feature?.Tick(this, Time.deltaTime);
    }

    protected override void OnStatsChanged()
    {
        base.OnStatsChanged();

        // Only on a change: features that apply effects make EffectManager recalculate again.
        if (effectMultiplier.CurrentValue == lastMultiplier)
            return;

        lastMultiplier = effectMultiplier.CurrentValue;
        foreach (var feature in features)
            feature?.OnMultiplierChanged(this);
    }

    protected override void OnDestroy()
    {
        foreach (var feature in features)
            feature?.OnDestroy(this);
        base.OnDestroy();
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(effectMultiplier);
        foreach (var feature in features)
            feature?.CollectStats(stats);
    }
}
