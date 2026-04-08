using UnityEngine;
public class FollowPlayerModule : ControlModule
{
    public void FixedUpdate()
    {
        var playerPosition = (ship as Enemy).Player.gameObject.transform.position;
        var clampedPosition = ClampPositionToCameraBounds(playerPosition);
        ship.SetTargetPosition(clampedPosition);
    }
}
