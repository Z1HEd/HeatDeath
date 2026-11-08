using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TeslaModule : WeaponModule, IHitter
{
    [SerializeField] protected ScalarStat damage = new ScalarStat(StatType.Damage, 15f, 0f);
    [SerializeField] protected ScalarStat shieldDamageMultiplier = new ScalarStat(StatType.ShieldDamageMultiplier, 1f, -1f);
    [SerializeField] protected ScalarStat hpDamageMultiplier = new ScalarStat(StatType.HPDamageMultiplier, 1f, -1f);
    [SerializeField] protected ScalarStat backstabMultiplier = new ScalarStat(StatType.BackstabMultiplier, 1f, 0f);
    [SerializeReference, SubclassSelector] private List<Effect> appliesEffects = new();

    public float KnockbackPower => 0f;
    public float Damage =>damage;
    public float ShieldDamageMultiplier => shieldDamageMultiplier;
    public float HPDamageMultiplier => hpDamageMultiplier;
    public float BackstabMultiplier => backstabMultiplier;
    public List<Effect> GetEffects(){ return EmitEffects(appliesEffects);}
    [SerializeField] private int maxChainTargets = 3;
    [SerializeField] private float lightningDuration = 0.2f;
    [Tooltip("Seconds between re-rolls of the bolt shape.")]
    [SerializeField] private float jitterInterval = 0.03f;
    [SerializeField] private Material lightningMaterial;
    [Tooltip("Lightning drawn over the hull of each ship hit. Leave empty to turn it off.")]
    [SerializeField] private Material shipLightningMaterial;


    private const float CHAIN_RANGE_FALLOFF = 0.6f;

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = DetectLayer;
    }

    protected override void Update()
    {
        base.Update();

        if (rangeDetector == null)
            return;
        var currentTarget = rangeDetector.GetClosestTarget();
        if (firePoint != null && currentTarget != null && !IsStunned)
        {
            Vector3 towardsTarget = currentTarget.transform.position - transform.position;
            Vector3 currentAim = firePoint.position - transform.position;

            float angle = Vector2.SignedAngle(currentAim, towardsTarget);
            transform.Rotate(0f, 0f, angle);
        }

        if (rangeDetector.GetClosestTarget(transform) != null && CanFire)
            Fire();
    }

    protected override void Fire()
    {
        base.Fire();

        List<Ship> chain = BuildChain();
        if (chain.Count == 0)
            return;

        ApplyChainDamage(chain);
        ShowLightning(chain);
    }

    private List<Ship> BuildChain()
    {
        var chain = new List<Ship>();
        float curRange = range;
        Vector2 fromPosition = transform.position;
        Ship next = rangeDetector.GetClosestTarget();
        List<Collider2D> hits;
        for (int i = 0; i < maxChainTargets; i++)
        {
            if (next == null) break;

            hits =new List<Collider2D>(Physics2D.OverlapCircleAll(next.transform.position, curRange, 1<<(DetectLayer-2)));
            hits.RemoveAll(col => col.GetComponent<Ship>() == null || chain.Contains(col.GetComponent<Ship>()));
            if (hits.Count<1) break;
            next = GetClosest(hits,next.transform.position).gameObject.GetComponent<Ship>();
            chain.Add(next);
            fromPosition = next.transform.position;
            curRange *= CHAIN_RANGE_FALLOFF;
        }

        return chain;
    }

    private void ApplyChainDamage(List<Ship> chain)
    {
        for (int i = 0; i < chain.Count; i++)
        {
            Ship target = chain[i];
            if (target == null || target.IsDead)
                continue;

            target.Hit(this);
        }
    }

    // One bolt per hop: fire point -> chain[0] -> chain[1] -> ...
    private void ShowLightning(List<Ship> chain)
    {
        if (lightningMaterial == null)
            return;

        // Draw above ships, on the weapon's own sorting layer.
        var sprite = GetComponent<SpriteRenderer>();
        int sortingLayerID = sprite != null ? sprite.sortingLayerID : 0;
        int sortingOrder = (sprite != null ? sprite.sortingOrder : 0) + 10;

        Transform from = firePoint != null ? firePoint : transform;
        foreach (Ship target in chain)
        {
            if (target == null)
                continue;

            LightningBolt.Spawn(from, from.position, target.transform, lightningDuration, jitterInterval,
                lightningMaterial, sortingLayerID, sortingOrder);

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

        stats.Add(damage);
        stats.Add(shieldDamageMultiplier);
        stats.Add(hpDamageMultiplier);
        stats.Add(backstabMultiplier);
    }
}
