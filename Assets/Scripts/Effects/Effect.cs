using System;
using System.Collections.Generic;
using UnityEngine;

// The effect is applied once to the ship when added
// It is however applied to a module each module recalculation
[Serializable]
public abstract class Effect
{
    [SerializeField]
    protected ResourceStat duration = new ResourceStat(StatType.Duration,float.PositiveInfinity,0f);
    public virtual bool Update(float delta)
    {
        duration.Consume(delta);
        return duration > 0f;
    }
    public abstract bool IsApplicableTo(ModuleDefinition definition);
    public abstract void ApplyToModule(ModuleBase module);
    public abstract void ApplyToShip(Ship ship);
    public abstract void RemoveFromShip(Ship ship);
    public abstract Effect Stacked(float times);
    public static List<Effect> StackedList(List<Effect> effects,float times)
    {
        var result = new List<Effect>();
        foreach (var effect in effects)
            result.Add(effect.Stacked(times));
        
        return result;
    }
}