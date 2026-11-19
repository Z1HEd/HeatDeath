using System;
using UnityEngine;

// Multiplies the ship's velocity once, when added, and then ends, whatever its duration. A lingering on-hit copy
// would keep the target debuffed for damage bonuses.
[Serializable]
public class MultiplyRigidbodyVelocityEffect : Effect
{
    [SerializeField]
    private float multiplier;

    public override void ApplyToShip(Ship ship)
    {
        ship.GetComponent<Rigidbody2D>().linearVelocity *= multiplier;
    }

    public override bool Update(float delta) => false;

    protected override Effect Stacked(float times)
    {
        var copy = (MultiplyRigidbodyVelocityEffect)base.Stacked(times);
        copy.multiplier = Mathf.Pow(multiplier, times);
        return copy;
    }
}
