using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected Ship ship;
    protected UpgradeManager upgradeManager;
    public ModuleDefinition ModuleDefinition => moduleDefinition;

    protected virtual void Awake()
    {
        ship = GetComponentInParent<Ship>();
        if (!ship) 
        {
            Debug.LogError("Module not in ship!");
            return;
        }
        ResetModifiers();
    }
    protected virtual void Start()
    {
        ship.moduleManager.AddModule(this);
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
    }

    protected IReadOnlyDictionary<StatType, StatModifier> GetApplicableModifiers(IReadOnlyList<Effect> effects)
    {
        var result = new Dictionary<StatType, StatModifier>();
        if (moduleDefinition == null) return result;
        foreach (var effect in effects)
        {
            if (!effect.IsApplicableTo(moduleDefinition))
                continue;

            effect.ApplyToMap(result);
        }

        return result;
    }

    public void UpdateEffects(IReadOnlyList<Effect> effects)
    {
        ResetModifiers();
        ApplyModifiers(GetApplicableModifiers(effects));
    }

    protected abstract void ResetModifiers();

    protected abstract void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifier> modifiers);
}
