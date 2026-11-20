using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lightning that never misses: a bolt to the closest enemy that jumps on to the closest ship not yet hit. Evolutions
// that strike differently (ShockFieldModule, ThunderSpearModule) override PickTarget and Strike.
public class TeslaModule : WeaponModule
{
    // Damage, the multipliers, and knockback (0: the bolt doesn't push) come from WeaponModule.
    [Header("Lightning")]
    [Tooltip("Ships one bolt hits, the first included. Evolutions repeat the strike instead. Rounded down.")]
    [SerializeField] protected ScalarStat chainTargets = new ScalarStat(StatType.ChainTargets, 3f, 1f);
    [Tooltip("The first jump reaches this share of the weapon's range, each further jump this share of the one " +
             "before.")]
    [SerializeField] protected ScalarStat chainJumpRange = new ScalarStat(StatType.ChainJumpRange, 0.6f, 0f);
    [Tooltip("Each jump, or each repeated strike, deals this share of the damage of the hit before it.")]
    [SerializeField] protected ScalarStat chainJumpDamage = new ScalarStat(StatType.ChainJumpDamage, 1f, 0f);
    [Tooltip("Percent of the Tesla's damage that an enemy it kills releases as bolts to the closest enemies around " +
             "it, without on-hit effects. Their deaths discharge again. 0: off.")]
    [SerializeField] protected ScalarStat dischargeDamage = new ScalarStat(StatType.DischargeDamage, 0f, 0f);
    [Tooltip("Enemies one discharge strikes, within the first jump's reach. Rounded down.")]
    [SerializeField] protected ScalarStat dischargeTargets = new ScalarStat(StatType.DischargeTargets, 2f, 0f);

    [Header("Visuals")]
    [SerializeField] private float lightningDuration = 0.2f;
    [Tooltip("Seconds between re-rolls of the bolt shape.")]
    [SerializeField] private float jitterInterval = 0.03f;
    [SerializeField] private Material lightningMaterial;
    [Tooltip("Lightning drawn over the hull of each ship hit. Leave empty to turn it off.")]
    [SerializeField] private Material shipLightningMaterial;
    [Tooltip("Lightning for Discharge bolts, so they stand apart from the chain. Empty: the normal lightning.")]
    [SerializeField] private Material dischargeLightningMaterial;

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = DetectLayer;
    }

    protected override void Update()
    {
        base.Update();

        Ship target = PickTarget();
        if (target == null)
            return;

        AimAt(target);
        if (CanFire)
        {
            Fire();
            // One payload for the whole strike, repeats included: a strike doesn't change once fired.
            Strike(target, CreatePayload());
        }
    }

    protected virtual Ship PickTarget() => rangeDetector.GetClosestTarget();

    protected virtual void Strike(Ship target, in HitPayload payload)
    {
        List<Ship> chain = BuildChain(target);
        ApplyChainDamage(chain, payload, 1f);
        ShowLightning(chain);
    }

    // For evolutions that repeat their strike: Chain Targets strikes, the interval apart, each weaker by Chain Jump
    // Damage. The strike gets its index and damage share, and returns false to stop early.
    protected IEnumerator RepeatStrikes(float interval, Func<int, float, bool> strike)
    {
        int strikes = Mathf.FloorToInt(chainTargets);
        float damageScale = 1f;
        for (int i = 0; i < strikes; i++)
        {
            if (i > 0)
                yield return new WaitForSeconds(interval);
            if (!strike(i, damageScale))
                yield break;
            damageScale *= chainJumpDamage;
        }
    }

    // Every hit the Tesla lands comes back here: bolts, blasts, and discharges alike.
    public override void OnHitLanded(ShipCoreModule target, in DamageTaken taken)
    {
        base.OnHitLanded(target, taken);
        if (taken.Killed && dischargeDamage > 0f && target != null)
            Discharge(target.Ship);
    }

    private void Discharge(Ship killed)
    {
        Vector2 at = killed.transform.position;
        var targets = new List<Ship>();
        foreach (Collider2D col in Physics2D.OverlapCircleAll(at, range * chainJumpRange, 1 << (DetectLayer - 2)))
        {
            Ship ship = col.GetComponent<Ship>();
            if (ship != null && !ship.IsDead && !targets.Contains(ship))
                targets.Add(ship);
        }
        targets.Sort((a, b) => ((Vector2)a.transform.position - at).sqrMagnitude
            .CompareTo(((Vector2)b.transform.position - at).sqrMagnitude));

        HitPayload payload = CreatePayload();
        int count = Mathf.Min(targets.Count, Mathf.FloorToInt(dischargeTargets));
        for (int i = 0; i < count; i++)
        {
            Ship target = targets[i];
            if (target.IsDead)
                continue;

            var bolt = new List<Ship> { target };
            ShowLightning(bolt, killed.transform, dischargeLightningMaterial);
            DamageTaken taken = target.TakeHit(new Hit(payload, at, at, Vector2.zero, 0, null, dischargeDamage / 100f));
            OnHitLanded(target.CoreModule, taken);
        }
    }

    // From the first target, each jump goes to the closest living ship not yet hit, within a range that shrinks per
    // jump.
    private List<Ship> BuildChain(Ship first)
    {
        var chain = new List<Ship> { first };
        float curRange = range * chainJumpRange;
        int maxTargets = Mathf.FloorToInt(chainTargets);
        while (chain.Count < maxTargets)
        {
            Vector3 lastPosition = chain[chain.Count - 1].transform.position;
            var hits = new List<Collider2D>(Physics2D.OverlapCircleAll(lastPosition, curRange, 1 << (DetectLayer - 2)));
            hits.RemoveAll(col =>
            {
                Ship ship = col.GetComponent<Ship>();
                return ship == null || ship.IsDead || chain.Contains(ship);
            });
            if (hits.Count < 1)
                break;

            chain.Add(GetClosest(hits, lastPosition).GetComponent<Ship>());
            curRange *= chainJumpRange;
        }

        return chain;
    }

    // Each ship is hit from the previous link of the chain (the fire point for the first), which sets backstab and
    // the shield ripple. Each jump deals Chain Jump Damage of the hit before it.
    protected void ApplyChainDamage(List<Ship> chain, in HitPayload payload, float damageScale)
    {
        Vector2 firedFrom = firePoint.position;
        Vector2 from = firedFrom;
        int enemiesHit = 0;
        HitEffects effects = GetHitEffects();
        foreach (Ship target in chain)
        {
            if (target == null || target.IsDead)
                continue;

            DamageTaken taken = target.TakeHit(new Hit(payload, firedFrom, from, Vector2.zero, enemiesHit, effects,
                damageScale));
            OnHitLanded(target.CoreModule, taken);
            enemiesHit++;
            from = target.transform.position;
            damageScale *= chainJumpDamage;
        }
    }

    // One bolt per hop: start (the fire point by default) -> chain[0] -> chain[1] -> ..., in the given material or
    // the normal lightning.
    protected void ShowLightning(List<Ship> chain, Transform start = null, Material material = null)
    {
        Material boltMaterial = material != null ? material : lightningMaterial;
        if (boltMaterial == null)
            return;

        // Draw above ships, on the weapon's own sorting layer.
        var sprite = GetComponent<SpriteRenderer>();
        int sortingLayerID = sprite != null ? sprite.sortingLayerID : 0;
        int sortingOrder = (sprite != null ? sprite.sortingOrder : 0) + 10;

        Transform from = start != null ? start : firePoint;
        foreach (Ship target in chain)
        {
            if (target == null)
                continue;

            LightningBolt.Spawn(from, from.position, target.transform, lightningDuration, jitterInterval,
                boltMaterial, sortingLayerID, sortingOrder);

            if (shipLightningMaterial != null && !target.IsDead)
                ShipLightningOverlay.Spawn(target, lightningDuration, jitterInterval,
                    shipLightningMaterial, sortingLayerID, sortingOrder);

            from = target.transform;
        }
    }

    Collider2D GetClosest(List<Collider2D> hits, Vector3 pos)
    {
        if (hits.Count == 0) return null;

        Collider2D closest = hits[0];
        float sqrDist = (closest.transform.position - pos).sqrMagnitude;
        foreach (Collider2D col in hits)
        {
            float currentDist = (col.transform.position - pos).sqrMagnitude;
            if (currentDist < sqrDist)
            {
                closest = col;
                sqrDist = currentDist;
            }
        }
        return closest;
    }
    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(chainTargets);
        stats.Add(chainJumpRange);
        stats.Add(chainJumpDamage);
        stats.Add(dischargeDamage);
        stats.Add(dischargeTargets);
    }
}
