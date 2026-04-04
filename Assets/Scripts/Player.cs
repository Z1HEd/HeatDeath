using UnityEngine;


public class Player : Ship
{

    void OnGUI()
    {
        if (!Event.current.isMouse || Event.current.button != 0)
            return; 
        var mousePosition = Event.current.mousePosition;
        foreach (var module in moduleManager.movementModules)
        {
            module.targetPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            module.targetPosition.y = -module.targetPosition.y;
        }
    }
}
