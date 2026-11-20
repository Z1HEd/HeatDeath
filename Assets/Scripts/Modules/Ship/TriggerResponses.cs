using System;
using System.Collections.Generic;
using UnityEngine;

// What a Triggered feature does when it fires, picked per module with SubclassSelector.
[Serializable]
public abstract class TriggerResponse
{
    public abstract void Run(ShipModule module);
    public virtual void OnMultiplierChanged(ShipModule module) {}
    public virtual void OnDestroy(ShipModule module) {}
}

// A timed buff. Running it again while it lasts restarts it instead of stacking: see EffectManager.SetEffects.
// The class keeps its name: assets store it.
[Serializable]
public class ApplyEffects : TriggerResponse
{
    [Tooltip("Give each effect a duration: the buff lasts as long as they do. Duration upgrades on the module scale them.")]
    [SerializeReference, SubclassSelector] private List<Effect> effects = new();

    public override void Run(ShipModule module)
    {
        foreach (var effect in effects)
            if (effect != null && float.IsPositiveInfinity(effect.Duration))
                Debug.LogWarning($"{module.ModuleDefinition.DisplayName}: a triggered effect has no duration, so it never ends", module);

        module.Ship.EffectManager.SetEffects(this, module.Emit(effects));
    }

    // Brings a running buff to the new strength, which also restarts it.
    public override void OnMultiplierChanged(ShipModule module)
    {
        if (module.Ship.EffectManager.HasEffects(this))
            Run(module);
    }

    public override void OnDestroy(ShipModule module) => module.Ship.EffectManager.ClearEffects(this);
}
