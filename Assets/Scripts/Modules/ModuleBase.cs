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

    protected virtual void Awake()
    {
        ship = GetComponentInParent<Ship>();
        if (!ship) ship = GetComponent<Ship>();
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


    public void UpdateEffects(IReadOnlyList<Effect> effects)
    {
        ResetModifiers();

        foreach (var effect in effects)
            effect.ApplyToModule(this);
        
        ApplyModifiers();
    }

    protected virtual void ResetModifiers() {currentModifiers.Clear();}

    protected virtual void ApplyModifiers() {}
}
