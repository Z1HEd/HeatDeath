using UnityEngine;
public class KeepDistanceModule : ControlModule
{
    [SerializeField]
    public float distanceMin = 4;
    public float distanceMax = 6;
    public void FixedUpdate()
    {
        if ((ship as Enemy).Player == null) return;
        var playerPosition = (ship as Enemy).Player.transform.position;
        var fromPlayer = transform.position - playerPosition;

        float distance = fromPlayer.magnitude;
        if (distance < 0.01f) return;

        if (distance > distanceMax || distance < distanceMin)
        {
            var desiredDistance = (distanceMin + distanceMax) / 2f;
            var targetPosition = playerPosition + fromPlayer.normalized * desiredDistance;
            var clampedPosition = ClampPositionToCameraBounds(targetPosition);

            ship.SetTargetPosition(clampedPosition);
        }
    }
}
