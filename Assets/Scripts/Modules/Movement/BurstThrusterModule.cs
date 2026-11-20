using UnityEngine;
using System.Collections.Generic;

public class BurstThrusterModule : MovementModule
{
    [Header("Steering Settings")]
    [SerializeField] private ScalarStat steeringThrust = new ScalarStat(StatType.Thrust, 0.05f, 0f);

    [Header("Burst Settings")]
    [SerializeField] private ScalarStat burstCooldown = new ScalarStat(StatType.Cooldown, 3f, 0f);

    private const float ThrustScale        = 100.0f;
    private const float StopDistance       = 0.2f;
    private const float BrakingSafety      = 0.9f;
    private const float SteeringSpeedRatio = 0.25f;

    private float cooldownTimer = 0f;

    private void Update()
    {
        if (steeringThrust <= 0f) // Stunned (thrust cut): no burst, and the cooldown pauses
            return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer > 0f || Time.timeScale <= 0f)
            return;

        Vector2 target = targetPosition;
        if (ship is Player && !TryGetPressedPoint(out target))
            return;

        Vector2 toTarget = target - body.position;
        if (toTarget.magnitude > StopDistance*maxSpeed)
        {
            body.linearVelocity = toTarget.normalized * maxSpeed;
            cooldownTimer = burstCooldown;
        }
    }

    // The player bursts toward where a click or touch lands this frame, outside the UI.
    private static bool TryGetPressedPoint(out Vector2 point)
    {
        point = default;
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame || GameUIController.Instance.IsCursorOverUI())
            return false;

        point = Camera.main.ScreenToWorldPoint(pointer.position.ReadValue());
        return true;
    }


    private void FixedUpdate()
    {
        if (ship != null && ship.IsKnockedBack)
            return;

        Vector2 toTarget         = targetPosition - body.position;
        float   dist             = toTarget.magnitude;
        float   steeringMaxSpeed = maxSpeed * SteeringSpeedRatio;
        float   maxAccel         = (steeringThrust * ThrustScale) / Mathf.Max(body.mass, 0.0001f);

        if (dist <= StopDistance)
        {
            Vector2 stopAccel = -body.linearVelocity / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            if (stopAccel.magnitude > maxAccel)
                stopAccel = stopAccel.normalized * maxAccel;

            body.AddForce(stopAccel * body.mass, ForceMode2D.Force);
            return;
        }

        float   allowedSpeed  = Mathf.Sqrt(2.0f * maxAccel * dist) * BrakingSafety;
        float   desiredSpeed  = Mathf.Min(steeringMaxSpeed, allowedSpeed);
        Vector2 desiredVel    = toTarget.normalized * desiredSpeed;
        Vector2 requiredAccel = (desiredVel - body.linearVelocity) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);

        if (requiredAccel.magnitude > maxAccel)
            requiredAccel = requiredAccel.normalized * maxAccel;

        body.AddForce(requiredAccel * body.mass, ForceMode2D.Force);
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(steeringThrust);
        stats.Add(burstCooldown);
    }
}