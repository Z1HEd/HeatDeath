using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[RequireComponent(typeof(Ship))]
public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    protected UpgradeManager upgradeManager;
    public ModuleDefinition ModuleDefinition => moduleDefinition;

    protected virtual void Start()
    {
        ship = GetComponent<Ship>();
        upgradeManager = GetComponent<UpgradeManager>();
        ship.AddModule(this);
        ResetModifiers();
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
    }

    protected IReadOnlyDictionary<StatType, StatModifierAggregate> GetCurrentModifiers()
    {
        var result = new Dictionary<StatType, StatModifierAggregate>();
        if (upgradeManager == null || moduleDefinition == null)
            return result;

        IReadOnlyList<ActiveStatModifier> effects = upgradeManager.ActiveEffects;
        for (int i = 0; i < effects.Count; i++)
        {
            ActiveStatModifier effect = effects[i];
            if (!effect.AppliesToModule(moduleDefinition))
                continue;

            AddModifier(result, effect);
        }

        return result;
    }

    public virtual void UpdateModifiers()
    {
        ResetModifiers();
        ApplyModifiers(GetCurrentModifiers());
    }

    private static void AddModifier(Dictionary<StatType, StatModifierAggregate> map, ActiveStatModifier modifierData)
    {
        StatModifier data = modifierData.Modifier;
        if (!map.TryGetValue(data.stat, out StatModifierAggregate modifier))
            modifier = default;

        if (data.operation == ModifierOperation.AddPercent)
            modifier.Percent += data.value;
        else
            modifier.Flat += data.value;

        map[data.stat] = modifier;
    }

    protected abstract void ResetModifiers();

    protected abstract void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers);
}
