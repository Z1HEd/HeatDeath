using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RangeDetector))]
public class ProjectileModule : WeaponModule
{
    [SerializeField] protected Projectile projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected ScalarStat projectileSpeed = new ScalarStat(StatType.ProjectileSpeed, 20f, 0f);
    [SerializeField] protected ScalarStat projectileDamage = new ScalarStat(StatType.ProjectileDamage, 10f, 0f);
    [SerializeField] protected ScalarStat projectileKnockback = new ScalarStat(StatType.ProjectileKnockback, 0f, 0f);
    [SerializeField] protected ScalarStat projectileCount = new ScalarStat(StatType.ProjectileCount, 1f, 1f);
    [SerializeField] protected ScalarStat projectileSpread = new ScalarStat(StatType.ProjectileSpread, 0f, 0f);
    [SerializeField] protected ScalarStat range = new ScalarStat(StatType.Range, 15f, 0f);
    [SerializeField] protected BoolStat canAim = new BoolStat(StatType.CanAim, true);
    private RangeDetector rangeDetector;
    private Ship currentTarget;

    public float Range => range;

    protected override void Start()
    {
        base.Start();
        rangeDetector = GetComponent<RangeDetector>();
        gameObject.layer = DetectLayer;
        rangeDetector.Initialize(range);
        rangeDetector.OnShipExitedRange += OnShipExitedRange;
    }


    protected override void Update()
    {
        base.Update();

        if (rangeDetector == null)
            return;

        // If no target, try to acquire one
        if (currentTarget == null)
        {
            currentTarget = rangeDetector.GetClosestTarget(transform);
        }

        // If we have a target, try to fire at it
        if (currentTarget != null && CanFire)
        {
            lastFireTime = Time.time;
            Fire();
        }
    }

    private void OnShipExitedRange(Ship exitedShip)
    {
        if (exitedShip == currentTarget)
        {
            currentTarget = null;
        }
    }

    protected override void Fire()
    {
        base.Fire();
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector3 aimDirection = canAim
            ? (currentTarget.transform.position - spawnPosition).normalized
            : (firePoint != null ? firePoint.up : transform.up);
        SpawnProjectiles(spawnPosition, aimDirection);
    }

    private void SpawnProjectiles(Vector3 spawnPosition, Vector3 aimDirection)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab not assigned on " + gameObject.name);
            return;
        }

        int count = Mathf.Max(1, Mathf.FloorToInt(projectileCount));
        float spreadDegrees = Mathf.Max(0f, projectileSpread);
        for (int i = 0; i < count; i++)
        {
            float angleOffset = GetSpreadAngleOffset(spreadDegrees);
            Vector3 shotDirection = Quaternion.AngleAxis(angleOffset, Vector3.forward) * aimDirection;

            Projectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            projectile.gameObject.layer = DetectLayer;

            projectile.Initialize(shotDirection * projectileSpeed, projectileDamage, projectileKnockback);
        }
    }

    private static float GetSpreadAngleOffset(float spreadDegrees)
    {
        if (spreadDegrees <= 0f)
            return 0f;

        float halfSpread = spreadDegrees * 0.5f;
        return Random.Range(-halfSpread, halfSpread);
    }

    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        base.ApplyModifiers(modifiers);
        projectileSpeed.Recalculate(modifiers);
        projectileDamage.Recalculate(modifiers);
        projectileKnockback.Recalculate(modifiers);
        projectileCount.Recalculate(modifiers);
        projectileSpread.Recalculate(modifiers);
        range.Recalculate(modifiers);
        canAim.Recalculate(modifiers);

        if (rangeDetector != null)
            rangeDetector.SetRadius(range);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();

        projectileSpeed.ResetToBase();
        projectileDamage.ResetToBase();
        projectileKnockback.ResetToBase();
        projectileCount.ResetToBase();
        projectileSpread.ResetToBase();
        range.ResetToBase();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (rangeDetector != null) Destroy(rangeDetector.gameObject);
    }
}

