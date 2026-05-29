using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RangeDetector))]
public class TeslaModule : WeaponModule, IHitter
{
    [SerializeField] private ScalarStat damage = new ScalarStat(StatType.ProjectileDamage, 15f, 0f);
    [SerializeField] private ScalarStat range = new ScalarStat(StatType.Range, 15f, 0f);
    [SerializeField] private int maxChainTargets = 3;
    [SerializeField] private float lightningDuration = 0.1f;
    [SerializeField] private Material lightningMaterial;

    private RangeDetector rangeDetector;

    public float Damage => damage;
    public float KnockbackPower => 0f;

    protected override void Awake()
    {
        base.Awake();
        rangeDetector = GetComponent<RangeDetector>();
        gameObject.layer = DetectLayer;
        rangeDetector.Initialize(range);
    }

    protected override void Update()
    {
        base.Update();

        if (rangeDetector == null)
            return;

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
        var hit = new HashSet<Ship>();

        Vector2 fromPosition = transform.position;

        for (int i = 0; i < maxChainTargets; i++)
        {
            Ship next = rangeDetector.GetClosestTargetTo(fromPosition, hit);
            if (next == null)
                break;

            chain.Add(next);
            hit.Add(next);
            fromPosition = next.transform.position;
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
        Vector3 previous = transform.position;

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

    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        base.ApplyModifiers(modifiers);
        damage.Recalculate(modifiers);
        range.Recalculate(modifiers);

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
