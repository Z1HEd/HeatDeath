using System;
using UnityEngine;

// Multiplies the ship's velocity once, when added.
[Serializable]
public class MultiplyRigidbodyVelocityEffect : Effect
{
    [SerializeField]
    private float multiplier;

    public override void ApplyToShip(Ship ship)
    {
        ship.GetComponent<Rigidbody2D>().linearVelocity *= multiplier;
    }

    protected override Effect Stacked(float times)
    {
        var copy = (MultiplyRigidbodyVelocityEffect)base.Stacked(times);
        copy.multiplier = Mathf.Pow(multiplier, times);
        return copy;
    }
}
