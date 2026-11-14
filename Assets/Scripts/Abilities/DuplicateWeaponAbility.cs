using UnityEngine;
using System.Collections.Generic;
using System;
public class DuplicateWeaponAbility : AbilityModule
{
    private WeaponModule spawnedWeapon;
    public override float CooldownFraction {get {return spawnedWeapon != null ? spawnedWeapon.CooldownFraction : 0f;}}
    protected override void Awake()
    {
        base.Awake();
        ship.moduleManager.OnWeaponAdded += DuplicateWeapon;
        ship.moduleManager.OnModuleReplaced += FollowReplacement;
    }
    protected void DuplicateWeapon(WeaponDefinition definition)
    {
        ship.moduleManager.OnWeaponAdded -= DuplicateWeapon;
        SetCopy(ship.moduleManager.AddWeapon(definition));
    }
    // The copy evolves along with the weapon it copies.
    private void FollowReplacement(ModuleBase old, ModuleBase replacement)
    {
        if (old == spawnedWeapon)
            SetCopy(replacement as WeaponModule);
    }
    private void SetCopy(WeaponModule weapon)
    {
        spawnedWeapon = weapon;
        icon = spawnedWeapon.GetComponent<SpriteRenderer>().sprite.texture;
        InvokeUpdateIcon();
    }
    public void Update()
    {
        if (spawnedWeapon)
            InvokeUpdateCooldown();
    }
}