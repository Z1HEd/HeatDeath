using UnityEngine;
using System.Collections.Generic;

public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    public ModuleDefinition ModuleDefinition => moduleDefinition;
    public Dictionary<StatType, StatModifier> currentModifiers = new Dictionary<StatType, StatModifier>();
    // On-hit effects that effects (upgrades) give this module, rebuilt on every recalculation like the modifiers.
    private readonly List<HitEffectsEffect> grantedHitEffects = new List<HitEffectsEffect>();
    private readonly List<IStat> collectedStats = new List<IStat>();

    protected virtual void Awake()
    {
        // Before the ship check: [ExecuteAlways] weapons run in edit mode without a ship.
        CollectStats(collectedStats);
        foreach (var stat in collectedStats)
            stat.Initialize();

        ship = GetComponentInParent<Ship>();
        if (!ship) ship = GetComponent<Ship>();
        if (!ship)
        {
            Debug.LogError("Module not in ship!");
            return;
        }
    }
    protected virtual void Start()
    {
        ship.moduleManager.AddModule(this);
        ship.effectManager.MarkDirty();
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
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
            result.Add(copy);
        }
        return result;
    }

    public void GrantHitEffects(HitEffectsEffect granted) => grantedHitEffects.Add(granted);

    // Copies for one shot or hit: the module's own on-hit effects (times the strength) plus the granted ones,
    // each granted set rolled against its chance.
    protected List<Effect> EmitHitEffects(IReadOnlyList<Effect> own, float times = 1f)
    {
        List<Effect> result = EmitEffects(own, times);
        foreach (var granted in grantedHitEffects)
            if (Random.value < granted.Chance)
                result.AddRange(EmitEffects(granted.Effects));
        return result;
    }

    // Every stat that effects can change has to be added here.
    protected virtual void CollectStats(List<IStat> stats) {}

    // Runs after effects change the stats, for work that depends on the new values.
    protected virtual void OnStatsChanged() {}
}
