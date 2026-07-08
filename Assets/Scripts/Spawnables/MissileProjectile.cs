using UnityEngine;
[ExecuteAlways]
public class MissileProjectile : Projectile
{
    [SerializeField] private float lockDelay = 0.5f;
    [SerializeField] private float turnSpeedDegrees = 540f;
    [SerializeField] protected ScalarStat targetLockRange = new ScalarStat(StatType.Range, 5f, 0f);
    [SerializeField] protected float explosionRange = 1f;
    [SerializeField] protected Explosion explosionPrefab;

    private RangeDetector rangeDetector;
    private Ship target;
    private float lockTime;
    private float speed;

    public override void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        base.Initialize(velocity, sourceModule);
        EnsureRangeDetector();
        rangeDetector.gameObject.layer = gameObject.layer;
        

        lockTime = Time.time + Mathf.Max(0f, lockDelay);
        speed = Mathf.Max(0.01f, velocity.magnitude);
        target = null;

        if (velocity.sqrMagnitude > 0f)
            transform.up = velocity.normalized;

        if (sourceModule is MissileModule){
            explosionRange = (sourceModule as MissileModule).ExplosionRange;
            targetLockRange = (sourceModule as MissileModule).SeekingRange;
        }
        else
            Debug.LogError("Missile expects MissileModule, got: "+ sourceModule);
        
        rangeDetector.Initialize(targetLockRange);
        targetLockRange.CurrentValueChanged += UpdateRange;
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (Time.time < lockTime)
            return;

        target = rangeDetector != null ? rangeDetector.GetClosestTarget(transform) : null;

        if (target == null || rb == null)
            return;

        Vector2 toTarget = (Vector2)target.transform.position - rb.position;
        if (toTarget.sqrMagnitude <= 0f)
            return;

        Vector2 currentDirection = rb.linearVelocity.sqrMagnitude > 0f
            ? rb.linearVelocity.normalized
            : (Vector2)transform.up;

        float maxRadians = turnSpeedDegrees * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 steeredDirection3 = Vector3.RotateTowards(currentDirection, toTarget.normalized, maxRadians, 0f);
        Vector2 steeredDirection = ((Vector2)steeredDirection3).normalized;

        rb.linearVelocity = steeredDirection * speed;
        transform.up = steeredDirection;
    }

    protected override bool TryApplyHit(Collider2D collision)
    {
        if (isDead)
            return false;

        if (collision.gameObject == null)
            return false;

        var hittable = collision.GetComponent<IHittable>();
        if (hittable == null || hittable.IsDead)
            return false;

        Explosion explosion = Instantiate(explosionPrefab,transform.position,transform.rotation);
        explosion.Initialize(this, explosionRange, gameObject.layer);
            
        TryConsumeCharge();
        return true;
    }

    protected void OnValidate()
    {
        UpdateRange(targetLockRange);
    }
    private void UpdateRange(float range)
    {   
        if (rangeDetector)
            rangeDetector.SetRadius(range);
    }
    private void EnsureRangeDetector()
    {
        if (rangeDetector == null)
            rangeDetector = GetComponentInChildren<RangeDetector>(true);
        if (rangeDetector == null)
        {
            GameObject detectorObject = new GameObject("RangeDetector");
            detectorObject.transform.SetParent(transform, false);
            rangeDetector = detectorObject.AddComponent<RangeDetector>();
        }
    }
}
