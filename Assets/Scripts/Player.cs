using UnityEngine;
using UnityEngine.InputSystem;
public class Player : Ship
{
    public AbilityModule ability => GetComponent<AbilityModule>();
    protected override void Update()
    {   
        base.Update();
        if (Keyboard.current.spaceKey.wasPressedThisFrame && ability.IsActivatable)
        {
            ability.Activate();
        }
    }
}
