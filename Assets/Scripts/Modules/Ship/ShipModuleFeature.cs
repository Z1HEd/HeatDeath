using System;
using System.Collections.Generic;

// One thing a ship module does, picked per module in the inspector. ShipModule forwards its lifecycle,
// passing itself, since stats can be recalculated before OnStart. A feature applies its effects with
// itself as the source, so features of one module never clash in EffectManager.
[Serializable]
public abstract class ShipModuleFeature
{
    public virtual void OnStart(ShipModule module) {}
    public virtual void Tick(ShipModule module, float deltaTime) {}
    // Also runs once when the module is first calculated.
    public virtual void OnMultiplierChanged(ShipModule module) {}
    public virtual void OnDestroy(ShipModule module) {}

    // Stats that upgrades on the module's tag can change. Called before OnStart.
    public virtual void CollectStats(List<IStat> stats) {}
}
