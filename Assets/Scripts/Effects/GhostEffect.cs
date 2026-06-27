using System;
using UnityEngine;

[Serializable]
public class GhostEffect : Effect
{
    private int storedLayer = -1;
    public override bool IsApplicableTo(ModuleDefinition definition){return false;}
    public override void ApplyToModule(ModuleBase module) {}
    public override void ApplyToShip(Ship ship) 
    {
        storedLayer = ship.gameObject.layer;
        ship.gameObject.layer = LayerMask.NameToLayer("Ghost");
    }
    public override void RemoveFromShip(Ship ship) {ship.gameObject.layer = storedLayer;}
    public override Effect Stacked(float times){return new GhostEffect();}
}