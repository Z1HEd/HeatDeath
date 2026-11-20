using UnityEngine;
using UnityEngine.InputSystem;
public class Player : Ship
{
    public AbilityModule Ability { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Ability = GetComponent<AbilityModule>();
    }

    // Space is a desktop shortcut for the ability button; phones have no keyboard.
    protected override void Update()
    {
        base.Update();
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Ability.Activate();
    }
}
