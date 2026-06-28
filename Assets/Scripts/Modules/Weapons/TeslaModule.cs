using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TeslaModule : WeaponModule, IHitter
{
    [SerializeField] protected ScalarStat damage = new ScalarStat(StatType.Damage, 15f, 0f);
    [SerializeField] protected ScalarStat shieldDamageMultiplier = new ScalarStat(StatType.ShieldDamageMultiplier, 1f, -1f);
    [SerializeField] protected ScalarStat hpDamageMultiplier = new ScalarStat(StatType.HPDamageMultiplier, 1f, -1f);
    [SerializeField] protected ScalarStat backstabMultiplier = new ScalarStat(StatType.BackstabMultiplier, 1f, 0f);
    [SerializeField] private List<Effect> appliesEffects = new();

    public float KnockbackPower => 0f;
    public float Damage =>damage;
    public float ShieldDamageMultiplier => shieldDamageMultiplier;
    public float HPDamageMultiplier => shieldDamageMultiplier;
    public float BackstabMultiplier => backstabMultiplier;
    public List<Effect> GetEffects(){ return appliesEffects;}
    [SerializeField] private int maxChainTargets = 3;
    [SerializeField] private float lightningDuration = 0.1f;
    [SerializeField] private Material lightningMaterial;


    private const float CHAIN_RANGE_FALLOFF = 0.75f;

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
        if (firePoint != null && currentTarget != null)
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
        StartCoroutine(ShowLightning(chain));
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

    private IEnumerator ShowLightning(List<Ship> chain)
    {
        var renderers = new List<LineRenderer>();
        Vector3 previous = firePoint!=null? firePoint.transform.position : transform.position;

        for (int i = 0; i < chain.Count; i++)
        {
            if (chain[i] == null)
                continue;

            GameObject go = new GameObject("Lightning");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, previous);
            lr.SetPosition(1, chain[i].transform.position);
            lr.startWidth = 0.1f;
            lr.endWidth = 0.05f;
            lr.useWorldSpace = true;
            if (lightningMaterial != null)
                lr.material = lightningMaterial;

            renderers.Add(lr);
            previous = chain[i].transform.position;
        }

        yield return new WaitForSeconds(lightningDuration);

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                Destroy(renderers[i].gameObject);
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
    protected override void ApplyModifiers()
    {
        base.ApplyModifiers();
        damage.Recalculate(currentModifiers);
        range.Recalculate(currentModifiers);

        if (rangeDetector != null)
            rangeDetector.SetRadius(range);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();
        damage.ResetToBase();
        range.ResetToBase();
    }
}
