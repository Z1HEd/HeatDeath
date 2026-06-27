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
    }
    protected void DuplicateWeapon(WeaponDefinition definition)
    {
        ship.moduleManager.OnWeaponAdded -= DuplicateWeapon;
        spawnedWeapon = ship.moduleManager.AddWeapon(definition);
        icon = spawnedWeapon.GetComponent<SpriteRenderer>().sprite.texture;
        InvokeUpdateIcon();
    }
    public void Update()
    {
        if (spawnedWeapon)
            InvokeUpdateCooldown();
    }

    protected override void ApplyModifiers(){}

}