// What a shot carries, fixed when it's fired: its damage, damage types, bonuses, and who fired it.
// Nothing in it changes while the shot flies.
public readonly struct HitPayload
{
    public readonly float Damage;
    public readonly float ShieldMultiplier;
    public readonly float HullMultiplier;
    public readonly float BackstabMultiplier;
    public readonly float Knockback;
    public readonly DamageBonuses Bonuses;
    // The module the damage comes from, for statistics.
    public readonly ModuleDefinition Source;
    // The ship and the weapon that fired: the weapon is null for ramming and abilities. Either may be destroyed by
    // the time the shot lands.
    public readonly Ship Attacker;
    public readonly WeaponModule Weapon;
    // Focus fire when fired: the target the weapon last hit and its hits in a row. The bonus counts only against
    // that target.
    public readonly ShipCoreModule FocusTarget;
    public readonly int FocusStacks;

    public HitPayload(float damage, float shieldMultiplier, float hullMultiplier, float backstabMultiplier,
        float knockback, DamageBonuses bonuses, ModuleDefinition source, Ship attacker, WeaponModule weapon,
        ShipCoreModule focusTarget = null, int focusStacks = 0)
    {
        FocusTarget = focusTarget;
        FocusStacks = focusStacks;
        Damage = damage;
        ShieldMultiplier = shieldMultiplier;
        HullMultiplier = hullMultiplier;
        BackstabMultiplier = backstabMultiplier;
        Knockback = knockback;
        Bonuses = bonuses;
        Source = source;
        Attacker = attacker;
        Weapon = weapon;
    }

    // Typeless damage of a share of the target's max hull (damage over time, hazard zones): 1x to shields and hull,
    // no knockback, no weapon. Still credited to its source and attacker.
    public static HitPayload PercentOfMaxHull(float percent, ModuleDefinition source, Ship attacker) =>
        new HitPayload(0f, 1f, 1f, 1f, 0f, new DamageBonuses { PercentMaxHull = percent }, source, attacker, null);

    // Tells the weapon that fired, if it still exists, what a hit took: focus fire and momentum count landed hits.
    public void ReportHit(ShipCoreModule target, in DamageTaken taken)
    {
        if (Weapon != null)
            Weapon.OnHitLanded(target, taken);
    }
}
