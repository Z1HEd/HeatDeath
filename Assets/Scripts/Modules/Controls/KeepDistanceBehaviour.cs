using UnityEngine;
public class KeepDistanceBehaviour : ControlBehaviour
{
    [SerializeField]
    public float distanceMin = 4;
    public float distanceMax = 6;
    public void FixedUpdate()
    {
        Player player = (ship as Enemy).Player;
        if (player == null || player.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        var playerPosition = player.transform.position;
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
