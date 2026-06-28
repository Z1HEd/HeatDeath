using System;

// The effect is applied once to the ship when added
// It is however applied to a module each module recalculation
[Serializable]
public abstract class Effect
{
    public abstract bool IsApplicableTo(ModuleDefinition definition);
    public abstract void ApplyToModule(ModuleBase module);
    public abstract void ApplyToShip(Ship ship);
    public abstract void RemoveFromShip(Ship ship);
    public abstract Effect Stacked(float times);
}