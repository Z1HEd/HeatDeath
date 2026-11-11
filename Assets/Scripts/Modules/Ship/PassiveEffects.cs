using System;
using System.Collections.Generic;
using UnityEngine;

// Effects the ship has while the module is installed, scaled by the module's EffectMultiplier.
[Serializable]
public class PassiveEffects : ShipModuleFeature
{
    [SerializeReference, SubclassSelector] private List<Effect> effects = new();

    public override void OnMultiplierChanged(ShipModule module) =>
        module.Ship.effectManager.Apply(this, module.Emit(effects));

    public override void OnDestroy(ShipModule module) =>
        module.Ship.effectManager.Remove(this);
}
