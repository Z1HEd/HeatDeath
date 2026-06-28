using System;
using UnityEngine;

[Serializable]
public class MultiplyRigidbodyVelocityEffect : Effect
{
    [SerializeField]
    private float multiplier;
    public MultiplyRigidbodyVelocityEffect(){}
    public MultiplyRigidbodyVelocityEffect(float Multiplier)
    {
        multiplier = Multiplier;
    }
    public override bool IsApplicableTo(ModuleDefinition definition){return false;}
    public override void ApplyToModule(ModuleBase module) {}
    public override void ApplyToShip(Ship ship) 
    {
        ship.GetComponent<Rigidbody2D>().linearVelocity *=multiplier;
    }
    public override void RemoveFromShip(Ship ship) {}
    public override Effect Stacked(float times){return new MultiplyRigidbodyVelocityEffect(multiplier*times);}
}