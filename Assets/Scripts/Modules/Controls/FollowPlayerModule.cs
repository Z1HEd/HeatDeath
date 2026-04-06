using UnityEngine;
public class FollowPlayerModule : ControlModule
{
    public void FixedUpdate()
    {
        ship.SetTargetPosition((ship as Enemy).Player.gameObject.transform.position);
    }
}
