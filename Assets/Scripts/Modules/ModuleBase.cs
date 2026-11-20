using UnityEngine;
using System.Collections.Generic;

public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    public Ship Ship => ship;
    public ModuleDefinition ModuleDefinition => moduleDefinition;
    // The modifiers effects give this module's stats, rebuilt on every recalculation.
    private readonly Dictionary<StatType, StatModifier> currentModifiers = new Dictionary<StatType, StatModifier>();
    // On-hit effects that effects (upgrades) give this module, rebuilt on every recalculation like the modifiers.
    private readonly List<HitEffectsEffect> grantedHitEffects = new List<HitEffectsEffect>();
    private readonly List<IStat> collectedStats = new List<IStat>();

    protected virtual void Awake()
    {
        CollectStats(collectedStats);
        foreach (var stat in collectedStats)
            stat.Initialize();

        ship = GetComponentInParent<Ship>();
        if (!ship)
            Debug.LogError("Module not in ship!");
    }
    protected virtual void Start()
    {
        ship.ModuleManager.AddModule(this);
        ship.EffectManager.MarkDirty();
    }

    protected virtual void OnDestroy()
    {
        ship.ModuleManager.RemoveModule(this);
    }


    public void UpdateEffects(IReadOnlyCollection<Effect> effects)
    {
        currentModifiers.Clear();
        grantedHitEffects.Clear();
        foreach (var effect in effects)
            if (effect.IsApplicableTo(moduleDefinition))
                effect.ApplyToModule(this);

        foreach (var stat in collectedStats)
            stat.Recalculate(currentModifiers);

        OnStatsChanged();
    }

    // Combines with the stat's other modifiers: flats add, percents multiply.
    public void AddModifier(StatType stat, StatModifier modifier)
    {
        currentModifiers.TryGetValue(stat, out StatModifier current);
        currentModifiers[stat] = StatModifier.Combine(current, modifier);
    }

    // Copies effects for this module to hand out. Its Duration modifier scales the copies' durations.
    protected List<Effect> EmitEffects(IReadOnlyList<Effect> templates, float times = 1f)
    {
        currentModifiers.TryGetValue(StatType.Duration, out StatModifier durationModifier);

        var result = new List<Effect>(templates.Count);
        foreach (var template in templates)
        {
            if (template == null)
                continue;

            Effect copy = template.Copy(times);
            copy.ModifyDuration(durationModifier);
            copy.OnEmittedBy(this);
            result.Add(copy);
        }
        return result;
    }

    public void GrantHitEffects(HitEffectsEffect granted) => grantedHitEffects.Add(granted);

    // Copies for one shot: the module's own on-hit effects (times the strength) plus the granted sets with their
    // chances, which each hit rolls.
    protected HitEffects EmitHitEffects(IReadOnlyList<Effect> own, float times = 1f)
    {
        var granted = new List<(float chance, List<Effect> effects)>(grantedHitEffects.Count);
        foreach (var set in grantedHitEffects)
            granted.Add((set.Chance, EmitEffects(set.Effects)));
        return new HitEffects(EmitEffects(own, times), granted);
    }

    // Every stat that effects can change has to be added here.
    protected virtual void CollectStats(List<IStat> stats) {}

    // Runs after effects change the stats, for work that depends on the new values.
    protected virtual void OnStatsChanged() {}
}
