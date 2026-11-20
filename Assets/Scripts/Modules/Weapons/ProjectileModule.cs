using UnityEngine;
using System.Collections.Generic;

public class ProjectileModule : WeaponModule
{
    [Header("Projectiles")]
    [SerializeField] protected Projectile projectilePrefab;
    [SerializeField] protected ScalarStat projectileSpeed = new ScalarStat(StatType.ProjectileSpeed, 20f, 0f);
    [Tooltip("Enemies a shot passes through before it ends. -1: all of them.")]
    [SerializeField] protected ScalarStat projectilePiercing = new ScalarStat(StatType.ProjectilePiercing, 0f, -1f);
    [SerializeField] protected ScalarStat projectileCount = new ScalarStat(StatType.ProjectileCount, 1f, 1f);
    [SerializeField] protected ScalarStat projectileSpread = new ScalarStat(StatType.ProjectileSpread, 0f, 0f);
    [Tooltip("The whole volley fires this many times, in evenly spaced directions from the aim. Rounded down.")]
    [SerializeField] protected ScalarStat projectileDirections = new ScalarStat(StatType.ProjectileDirections, 1f, 1f);
    [Tooltip("Degrees the directions spread over: 360 or more goes around the circle, less is a fan centred on the aim.")]
    [SerializeField] protected ScalarStat directionArc = new ScalarStat(StatType.DirectionArc, 360f, 0f);

    private Ship currentTarget;

    public float Piercing => projectilePiercing;

    protected override void Awake()
    {
        base.Awake();
        rangeDetector.OnShipExitedRange += OnShipExitedRange;
    }


    protected override void Update()
    {
        base.Update();

        if (currentTarget == null)
            currentTarget = rangeDetector.GetClosestTarget();

        if (currentTarget == null)
            return;

        AimAt(currentTarget);
        if (CanFire)
            Fire();
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
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab not assigned on " + gameObject.name);
            return;
        }
        SpawnProjectiles(firePoint.position, (firePoint.position - transform.position).normalized);
    }

    // The volley: Projectile Count projectiles in each of the Projectile Directions.
    protected virtual void SpawnProjectiles(Vector3 position, Vector3 aimDirection)
    {
        int count = Mathf.Max(1, Mathf.FloorToInt(projectileCount));
        int directions = Mathf.Max(1, Mathf.FloorToInt(projectileDirections));
        for (int d = 0; d < directions; d++)
        {
            Vector3 volleyDirection = Quaternion.AngleAxis(DirectionAngle(d, directions), Vector3.forward) * aimDirection;
            for (int i = 0; i < count; i++)
                SpawnProjectile(position, volleyDirection, d, directions);
        }
    }

    // One projectile, turned off its direction by up to half the spread. It flies in the volley's direction number
    // `direction` of `directions` (for OnProjectileSpawned).
    protected void SpawnProjectile(Vector3 position, Vector3 aimDirection, int direction, int directions)
    {
        Vector3 shotDirection = Quaternion.AngleAxis(GetSpreadAngleOffset(projectileSpread), Vector3.forward) * aimDirection;
        Projectile projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
        projectile.gameObject.layer = DetectLayer;
        projectile.Initialize(shotDirection, projectileSpeed, this);
        OnProjectileSpawned(projectile, direction, directions);
    }

    // Evenly spaced: around the whole circle at 360° or more (8 directions: every 45°), otherwise a fan centred on the
    // aim whose outer directions sit on the arc's edges (3 over 45°: -22.5°, 0°, 22.5°).
    private float DirectionAngle(int index, int directions)
    {
        if (directions <= 1)
            return 0f;

        float arc = Mathf.Max(0f, directionArc);
        if (arc >= 360f)
            return 360f / directions * index;
        return -arc / 2f + arc / (directions - 1) * index;
    }

    // After Initialize, with which of the volley's directions it flies in (ProjectileDirections; 0 is the rightmost of
    // a fan).
    protected virtual void OnProjectileSpawned(Projectile projectile, int direction, int directions) { }

    private static float GetSpreadAngleOffset(float spreadDegrees)
    {
        if (spreadDegrees <= 0f)
            return 0f;

        float halfSpread = spreadDegrees * 0.5f;
        return Random.Range(-halfSpread, halfSpread);
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(projectileSpeed);
        stats.Add(projectilePiercing);
        stats.Add(projectileCount);
        stats.Add(projectileSpread);
        stats.Add(projectileDirections);
        stats.Add(directionArc);
    }

}