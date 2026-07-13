using System;
using UnityEngine;

[Serializable]
public class MultiplyRigidbodyVelocityEffect : Effect
{
    [SerializeField]
    private float multiplier;
    public MultiplyRigidbodyVelocityEffect(){}
    public MultiplyRigidbodyVelocityEffect(ResourceStat duration, float Multiplier)
    {
        this.duration = duration;
        multiplier = Multiplier;
    }
    public override bool IsApplicableTo(ModuleDefinition definition){return false;}
    public override void ApplyToModule(ModuleBase module) {}
    public override void ApplyToShip(Ship ship) 
    {
        ship.GetComponent<Rigidbody2D>().linearVelocity *=multiplier;
    }
    public override void RemoveFromShip(Ship ship) {}
    public override Effect Stacked(float times)
    {
        var durationCopy = new ResourceStat(duration.Type, duration.BaseValue, duration.MinValue);
        return new MultiplyRigidbodyVelocityEffect(durationCopy,multiplier*times);
    }
}