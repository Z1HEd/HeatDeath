using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    protected UpgradeManager upgradeManager;
    public ModuleDefinition ModuleDefinition => moduleDefinition;
    public Dictionary<StatType, StatModifier> currentModifiers = new Dictionary<StatType, StatModifier>();
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

            Effect copy = template.Stacked(times);
            copy.ModifyDuration(durationModifier);
            result.Add(copy);
        }
        return result;
    }

    // Every stat that effects can change has to be added here.
    protected virtual void CollectStats(List<IStat> stats) {}

    // Runs after effects change the stats, for work that depends on the new values.
    protected virtual void OnStatsChanged() {}
}
