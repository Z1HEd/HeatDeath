using UnityEngine;

public class FollowCursorModule : ControlModule
{
    void OnGUI()
    {
        if (!Event.current.isMouse || Event.current.button != 0)
            return;
        var mousePosition = Event.current.mousePosition;
        mousePosition.y = -mousePosition.y;
        var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        worldPosition.y += Camera.main.orthographicSize*2;
        var clampedPosition = ClampPositionToCameraBounds(worldPosition);
        ship.SetTargetPosition(clampedPosition);
    }
}
