using UnityEngine;
using System.Collections.Generic;
public abstract class MovementModule : ModuleBase
{
    [SerializeField] protected ScalarStat maxSpeed = new ScalarStat(StatType.MaxSpeed, 10f, 0f);
    [Tooltip("How fast the ship loses speed it doesn't thrust against (the rigidbody's linear damping). Debuffs " +
             "that leave a ship drifting raise it, so it slows down to a stop.")]
    [SerializeField] private ScalarStat drag = new ScalarStat(StatType.Drag, 0f, 0f);

    protected Rigidbody2D body;
    // Where the ship's controls want it to go.
    protected Vector2 targetPosition;

    // The ship's speed, and its max speed before effects: thruster particles scale against it.
    public float Speed => body.linearVelocity.magnitude;
    public float BaseMaxSpeed => maxSpeed.BaseValue;

    public void SetTargetPosition(Vector2 position) => targetPosition = position;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponentInParent<Rigidbody2D>();
        targetPosition = body.position;
    }
    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(maxSpeed);
        stats.Add(drag);
    }

    protected override void OnStatsChanged() => body.linearDamping = drag;
}
   