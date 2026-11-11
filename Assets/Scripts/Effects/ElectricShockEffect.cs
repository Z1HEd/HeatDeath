using System;
using UnityEngine;

// Brief electric shocks at random spots on the affected ship's hull, as if parts of it were
// malfunctioning. Each shock stays at its spot and flickers a few times.
[Serializable]
public class ElectricShockEffect : Effect
{
    [SerializeField] private Material material;
    [Tooltip("How far a shock's sparks can reach from its spot, in world units.")]
    [SerializeField] private float regionRadius = 0.28f;
    [Tooltip("Seconds between new shocks are picked at random between this and Max Interval.")]
    [SerializeField] private float minInterval = 0.08f;
    [SerializeField] private float maxInterval = 0.3f;
    [Tooltip("Seconds for one flicker, on and off.")]
    [SerializeField] private float flickerLength = 0.09f;
    [Tooltip("Each shock flickers between 1 and this many times.")]
    [SerializeField] private int maxFlickers = 3;

    private Ship target;
    private float timeToNextShock;

    public override void ApplyToShip(Ship ship)
    {
        target = ship;
        timeToNextShock = 0f;
    }

    public override void RemoveFromShip(Ship ship)
    {
        target = null;
    }

    public override bool Update(float delta)
    {
        bool stillActive = base.Update(delta);

        timeToNextShock -= delta;
        if (stillActive && timeToNextShock <= 0f && target != null && !target.IsDead && material != null)
        {
            timeToNextShock = UnityEngine.Random.Range(minInterval, maxInterval);
            ShipLightningOverlay.SpawnRegional(target, regionRadius, UnityEngine.Random.Range(1, maxFlickers + 1),
                flickerLength, material, SortingLayer.NameToID("Turrets"), 10);
        }

        return stillActive;
    }
}
