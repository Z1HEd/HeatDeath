using UnityEngine;
public class FollowPlayerBehaviour : ControlBehaviour
{
    public void FixedUpdate()
    {
        var playerPosition = (ship as Enemy).Player.gameObject.transform.position;
        var clampedPosition = ClampPositionToCameraBounds(playerPosition);
        ship.SetTargetPosition(clampedPosition);
    }
}
