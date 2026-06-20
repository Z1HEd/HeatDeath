using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class ProjectileModule : WeaponModule
{
    [SerializeField] public Projectile projectilePrefab;
    [SerializeField] public Transform firePoint;
    [SerializeField] public ScalarStat projectileSpeed = new ScalarStat(StatType.ProjectileSpeed, 20f, 0f);
    [SerializeField] public ScalarStat projectileDamage = new ScalarStat(StatType.Damage, 10f, 0f);
    [SerializeField] public ScalarStat ShieldDamageMultiplier = new ScalarStat(StatType.ShieldDamageMultiplier, 1f, -1f);
    [SerializeField] public ScalarStat HPDamageMultiplier = new ScalarStat(StatType.HPDamageMultiplier, 1f, -1f);
    [SerializeField] public ScalarStat projectileKnockback = new ScalarStat(StatType.ProjectileKnockback, 0f, 0f);
    [SerializeField] public ScalarStat projectilePiercing = new ScalarStat(StatType.ProjectilePiercing, 0f, -1f);
    [SerializeField] public ScalarStat projectileCount = new ScalarStat(StatType.ProjectileCount, 1f, 1f);
    [SerializeField] public ScalarStat projectileSpread = new ScalarStat(StatType.ProjectileSpread, 0f, 0f);
    [SerializeField] public BoolStat canAim = new BoolStat(StatType.CanAim, true);

    private Ship currentTarget;

    protected override void Awake()
    {
        base.Awake();
        rangeDetector.OnShipExitedRange += OnShipExitedRange;
        firePoint = transform.GetChild(0);
    }


    protected override void Update()
    {
        base.Update();

        if (rangeDetector == null)
            return;

        if (currentTarget == null)
            currentTarget = rangeDetector.GetClosestTarget(transform);
        
        if (currentTarget == null)
            return;

        if (firePoint != null && canAim)
        {
            Vector3 towardsTarget = currentTarget.transform.position - transform.position;
            Vector3 currentAim = firePoint.position - transform.position;

            float angle = Vector2.SignedAngle(currentAim, towardsTarget);
            transform.Rotate(0f, 0f, angle);
        }

        if (CanFire)
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
        Vector3 aimDirection = (firePoint.position - transform.position).normalized;
        SpawnProjectiles(spawnPosition, aimDirection);
    }

    protected virtual void SpawnProjectiles(Vector3 spawnPosition, Vector3 aimDirection)
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
            projectile.Initialize(shotDirection * projectileSpeed, this);
            OnProjectileSpawned(projectile);
        }
    }

    protected virtual void OnProjectileSpawned(Projectile projectile) { }

    protected static float GetSpreadAngleOffset(float spreadDegrees)
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
        projectilePiercing.Recalculate(modifiers);
        projectileCount.Recalculate(modifiers);
        projectileSpread.Recalculate(modifiers);
        canAim.Recalculate(modifiers);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();

        projectileSpeed.ResetToBase();
        projectileDamage.ResetToBase();
        projectileKnockback.ResetToBase();
        projectilePiercing.ResetToBase();
        projectileCount.ResetToBase();
        projectileSpread.ResetToBase();
        range.ResetToBase();
    }
    
}