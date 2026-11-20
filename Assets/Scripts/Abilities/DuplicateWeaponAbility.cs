using UnityEngine;
using System.Collections.Generic;
using System;
public class DuplicateWeaponAbility : AbilityModule
{
    private WeaponModule spawnedWeapon;
    public override float CooldownLeft => spawnedWeapon != null ? spawnedWeapon.CooldownLeft : 0f;
    protected override void Awake()
    {
        base.Awake();
        ship.ModuleManager.OnWeaponAdded += DuplicateWeapon;
        ship.ModuleManager.OnModuleReplaced += FollowReplacement;
    }
    protected void DuplicateWeapon(WeaponDefinition definition)
    {
        ship.ModuleManager.OnWeaponAdded -= DuplicateWeapon;
        SetCopy(ship.ModuleManager.AddWeapon(definition));
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
        RaiseIconChanged();
    }
    private void Update()
    {
        if (spawnedWeapon)
            RaiseCooldownChanged();
    }
}