using System.Collections.Generic;
using UnityEngine;

// Thunder Spear: locks onto the enemy in range with the highest max shields (the closest of equals) and holds it
// until it dies or leaves range. Chain targets beyond the first strike it again instead of jumping, each bolt weaker.
public class ThunderSpearModule : TeslaModule
{
    [Header("Thunder Spear")]
    [Tooltip("Seconds between the bolts of one strike.")]
    [SerializeField] private float boltInterval = 0.1f;

    private Ship lockedTarget;

    protected override void Awake()
    {
        base.Awake();
        rangeDetector.OnShipExitedRange += OnShipExitedRange;
    }

    protected override Ship PickTarget()
    {
        if (lockedTarget == null || lockedTarget.IsDead)
            lockedTarget = MostShieldedInRange();
        return lockedTarget;
    }

    protected override void Strike(Ship target, in HitPayload payload)
    {
        HitPayload shot = payload;
        StartCoroutine(RepeatStrikes(boltInterval, (_, damageScale) =>
        {
            if (target == null || target.IsDead)
                return false;

            var bolt = new List<Ship> { target };
            ApplyChainDamage(bolt, shot, damageScale);
            ShowLightning(bolt);
            return true;
        }));
    }

    private Ship MostShieldedInRange()
    {
        Ship best = null;
        int bestShields = -1;
        float bestSqr = float.MaxValue;
        foreach (Ship ship in rangeDetector.Targets)
        {
            if (ship.IsDead)
                continue;

            int shields = ship.CoreModule.CurrentMaxShields;
            float sqr = (ship.transform.position - transform.position).sqrMagnitude;
            if (shields > bestShields || (shields == bestShields && sqr < bestSqr))
            {
                best = ship;
                bestShields = shields;
                bestSqr = sqr;
            }
        }
        return best;
    }

    private void OnShipExitedRange(Ship ship)
    {
        if (ship == lockedTarget)
            lockedTarget = null;
    }
}
