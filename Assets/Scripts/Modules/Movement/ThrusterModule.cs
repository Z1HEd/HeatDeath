using UnityEngine;
using System.Collections.Generic;
public class ThrusterModule : MovementModule
{
    [Header("Thruster Settings")]
    [SerializeField] private ScalarStat thrust = new ScalarStat(StatType.Thrust, 0.5f, 0f);

    private const float ThrustScale = 100.0f;
    private const float StopDistance = 0.1f;
    private const float BrakingSafety = 0.9f;

    private void FixedUpdate()
    {
        if (ship != null && ship.IsKnockedBack)
            return;

        Vector2 toTarget = targetPosition - body.position;
        float dist = toTarget.magnitude;

        float maxAccel = (thrust * ThrustScale) / Mathf.Max(body.mass, 0.0001f);
        Vector2 desiredVelocity = Vector2.zero;
        Vector2 requiredAccel;

        // If basically already there, do a soft stop.
        if (dist <= StopDistance)
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
            allowedSpeed = Mathf.Sqrt(2.0f * maxAccel * dist) * BrakingSafety;
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

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(thrust);
    }
}