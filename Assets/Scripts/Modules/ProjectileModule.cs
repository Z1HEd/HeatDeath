using UnityEngine;

public class ProjectileModule : WeaponModule
{
    [SerializeField] protected Projectile projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float projectileSpeed = 20f;
    [SerializeField] protected float projectileDamage = 10f;
    [SerializeField] protected float projectileKnockback = 5f;
    [SerializeField] protected int projectileCount = 1;
    [SerializeField] protected float range = 15f;

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

        for (int i = 0; i < projectileCount; i++)
        {
            Projectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            projectile.gameObject.layer = DetectLayer;
            projectile.Initialize(aimDirection * projectileSpeed, projectileDamage, projectileKnockback);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (rangeDetector != null) Destroy(rangeDetector.gameObject);
    }
}

