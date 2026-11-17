using UnityEngine;

public class FollowCursorBehaviour : ControlBehaviour
{
    private bool isDragging = false;

    void OnGUI()
    {
        if (!Event.current.isMouse || Event.current.button != 0)
            return;

        EventType type = Event.current.type;

        if (type == EventType.MouseDown)
        {
            if (Time.timeScale <= 0f || GameUIController.Instance.IsCursorOverUI())
                return;

            isDragging = true;
        }
        else if (type == EventType.MouseUp)
        {
            isDragging = false;
            return;
        }

        if (!isDragging)
            return;

        var mousePosition = Event.current.mousePosition;
        mousePosition.y = Screen.height - mousePosition.y; // GUI y runs top-down, screen y bottom-up
        var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        var clampedPosition = ClampPositionToCameraBounds(worldPosition);
        ship.SetTargetPosition(clampedPosition);
    }
}