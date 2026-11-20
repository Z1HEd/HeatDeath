using UnityEngine;

public class MissileProjectile : Projectile
{
    [SerializeField] private float lockDelay = 0.5f;
    [SerializeField] private float turnSpeedDegrees = 540f;
    [SerializeField] protected Explosion explosionPrefab;

    // From the launcher, as fired.
    private float blastDiameter;
    private RangeDetector rangeDetector;
    private Ship target;
    private float lockTime;
    private float speed;

    public override void Initialize(Vector2 direction, float speed, ProjectileModule sourceModule)
    {
        base.Initialize(direction, speed, sourceModule);
        lockTime = Time.time + Mathf.Max(0f, lockDelay);
        this.speed = Mathf.Max(0.01f, speed);
        target = null;

        if (direction.sqrMagnitude > 0f)
            transform.up = direction;

        var launcher = sourceModule as MissileModule;
        if (launcher == null)
        {
            Debug.LogError("Missile expects MissileModule, got: " + sourceModule);
            return;
        }

        blastDiameter = launcher.BlastDiameter;
        rangeDetector = RangeDetector.FindOrCreate(transform);
        rangeDetector.gameObject.layer = gameObject.layer;
        rangeDetector.Initialize(launcher.SeekingRange);
    }

    private void FixedUpdate()
    {
        if (IsDead)
            return;

        if (Time.time < lockTime)
            return;

        target = rangeDetector.GetClosestTarget();

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

    // Explodes where it hits: the blast deals the damage.
    protected override void Strike(IHittable hittable, Collider2D collision)
    {
        Explosion explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.Initialize(Payload, FiredFrom, HitEffects, blastDiameter, gameObject.layer);
        LeaveHazard(explosion);
    }
}
