using UnityEngine;
public class FollowPlayerBehaviour : ControlBehaviour
{
    public void FixedUpdate()
    {
        Player player = (ship as Enemy).Player;
        if (player == null || player.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        
        var playerPosition = player.gameObject.transform.position;
        var clampedPosition = ClampPositionToCameraBounds(playerPosition);
        ship.SetTargetPosition(clampedPosition);
    }
}
