using UnityEngine;
public class ThrusterModule : MovementModule
{
    [Header("Thruster Settings")]
    [SerializeField] private float thrust = 0.5f;
    [SerializeField] private float maxSpeed = 10.0f;
    
    [Header("Internal")]
    [SerializeField] private float thrustScale = 100.0f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField, Range(0f, 1f)] private float brakingSafety = 0.9f;

    public void FixedUpdate()
    {
        if (ship != null && ship.IsKnockedBack)
            return;

        Vector2 toTarget = targetPosition - body.position;
        float dist = toTarget.magnitude;

        float maxAccel = (thrust * thrustScale) / Mathf.Max(body.mass, 0.0001f);
        Vector2 desiredVelocity = Vector2.zero;
        Vector2 requiredAccel;

        // If basically already there, do a soft stop.
        if (dist <= stopDistance)
        {
            requiredAccel = (desiredVelocity - body.linearVelocity) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);

            if (requiredAccel.magnitude > maxAccel)
                requiredAccel = requiredAccel.normalized * maxAccel;

            body.AddForce(requiredAccel * body.mass, ForceMode2D.Force);
            return;
        }

        float allowedSpeed = 0.0f;
        if (dist > 0.0f)
        {
            allowedSpeed = Mathf.Sqrt(2.0f * maxAccel * dist) * brakingSafety;
        }

        float desiredSpeed = Mathf.Min(maxSpeed, allowedSpeed);
        desiredVelocity = toTarget.normalized * desiredSpeed;

        Vector2 v = body.linearVelocity;
        requiredAccel = (desiredVelocity - v) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);

        if (requiredAccel.magnitude > maxAccel)
            requiredAccel = requiredAccel.normalized * maxAccel;

        body.AddForce(requiredAccel * body.mass, ForceMode2D.Force);

        if (body.linearVelocity.magnitude > maxSpeed)
            body.linearVelocity = body.linearVelocity.normalized * maxSpeed;
    }

    public void SetTargetPosition(Vector2 newTarget)
    {
        targetPosition = newTarget;
    }
}