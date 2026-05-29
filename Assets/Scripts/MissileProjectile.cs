using UnityEngine;
[ExecuteAlways]
public class MissileProjectile : Projectile
{
    [SerializeField] private float lockDelay = 0.5f;
    [SerializeField] private float turnSpeedDegrees = 540f;
    [SerializeField] protected ScalarStat range = new ScalarStat(StatType.Range, 5f, 0f);

    private RangeDetector rangeDetector;
    private Ship target;
    private float lockTime;
    private float speed;

    public override void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        base.Initialize(velocity, sourceModule);
        EnsureRangeDetector();
        rangeDetector.gameObject.layer = gameObject.layer;
        rangeDetector.Initialize(range);
        range.CurrentValueChanged += UpdateRange;

        lockTime = Time.time + Mathf.Max(0f, lockDelay);
        speed = Mathf.Max(0.01f, velocity.magnitude);
        target = null;

        if (velocity.sqrMagnitude > 0f)
            transform.up = velocity.normalized;
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (Time.time < lockTime)
            return;

        if (target == null || target.IsDead)
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
    protected void OnValidate()
    {
        UpdateRange(range);
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
