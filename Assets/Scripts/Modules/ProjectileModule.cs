using UnityEngine;

public class ProjectileModule : WeaponModule
{
    [SerializeField] protected Projectile projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected ScalarStat projectileSpeed = new ScalarStat(20f, 0f);
    [SerializeField] protected ScalarStat projectileDamage = new ScalarStat(10f, 0f);
    [SerializeField] protected ScalarStat projectileKnockback = new ScalarStat(0f, 0f);
    [SerializeField] protected ScalarStat projectileCount = new ScalarStat(1f, 1f);
    [SerializeField] protected ScalarStat range = new ScalarStat(15f, 0f);

    private RangeDetector rangeDetector;
    private Ship currentTarget;

    public float Range => range;

    protected override void Start()
    {
        base.Start();
        EnsureRangeDetector();
    }

    private void EnsureRangeDetector()
    {
        if (rangeDetector != null)
            return;

        var detectorObject = new GameObject($"RangeDetector");
        detectorObject.transform.SetParent(transform, false);
        detectorObject.transform.localPosition = Vector3.zero;

        detectorObject.layer = DetectLayer;
        rangeDetector = detectorObject.AddComponent<RangeDetector>();
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
        SpawnProjectiles(currentTarget);
    }

    private void SpawnProjectiles(Ship target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab not assigned on " + gameObject.name);
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector3 aimDirection = target != null
            ? (target.transform.position - spawnPosition).normalized
            : (firePoint != null ? firePoint.right : transform.right);

        int count = Mathf.Max(1, Mathf.FloorToInt(projectileCount));
        for (int i = 0; i < count; i++)
        {
            Projectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            projectile.gameObject.layer = DetectLayer;
            projectile.Initialize(aimDirection * projectileSpeed, projectileDamage, projectileKnockback);
        }
    }

    public override void Recalculate()
    {
        base.Recalculate();

        projectileSpeed.Recalculate(CurrentModifiers);
        projectileDamage.Recalculate(CurrentModifiers);
        projectileKnockback.Recalculate(CurrentModifiers);
        projectileCount.Recalculate(CurrentModifiers);
        range.Recalculate(CurrentModifiers);

        if (rangeDetector != null)
            rangeDetector.Initialize(range);
    }

    protected override void ResetValues()
    {
        base.ResetValues();

        projectileSpeed.ResetToBase();
        projectileDamage.ResetToBase();
        projectileKnockback.ResetToBase();
        projectileCount.ResetToBase();
        range.ResetToBase();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (rangeDetector != null) Destroy(rangeDetector.gameObject);
    }
}

