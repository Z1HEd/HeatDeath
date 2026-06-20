using UnityEngine;
using System.Collections.Generic;

public class DuplicateWeaponModule : ModuleBase
{
    protected override void Awake()
    {
        base.Awake();
        ship.moduleManager.OnWeaponAdded += DuplicateWeapon;
    }
    protected void DuplicateWeapon(WeaponDefinition definition)
    {
        ship.moduleManager.OnWeaponAdded -= DuplicateWeapon;
        ship.moduleManager.AddWeapon(definition);
    }

    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
    }

    protected override void ResetModifiers()
    {
    }
}