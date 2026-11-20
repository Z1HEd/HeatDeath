using System;
using UnityEngine;

// Damage over time (nanites, toxins): a share of the ship's max hull per second, dealt in ticks while it lasts, so it
// keeps up with enemy scaling. Typeless: shields and hull take it at 1x. Its damage counts for the module that handed
// it out. Give it a finite duration, as every on-hit effect needs.
[Serializable]
public class DamageOverTimeEffect : Effect
{
    [Tooltip("Percent of the ship's max hull per second. Stacks multiply it.")]
    [SerializeField] private float percentMaxHullPerSecond = 1f;

    [NonSerialized] private ModuleDefinition source;
    [NonSerialized] private Ship attacker;
    [NonSerialized] private Ship target;
    [NonSerialized] private DamageTicks ticks;

    public override void OnEmittedBy(ModuleBase module)
    {
        source = module.ModuleDefinition;
        attacker = module.Ship;
    }

    public override void ApplyToShip(Ship ship)
    {
        target = ship;
        ticks = default;
    }

    public override void RemoveFromShip(Ship ship) => target = null;

    public override bool Update(float delta)
    {
        for (int due = ticks.Advance(delta, TimeLeft); due > 0 && target != null && !target.IsDead; due--)
            Tick();
        return base.Update(delta);
    }

    // A hit from the ship's own position: no backstab, no knockback, no distance bonus.
    private void Tick()
    {
        var payload = HitPayload.PercentOfMaxHull(percentMaxHullPerSecond * DamageTicks.Interval, source, attacker);
        Vector2 at = target.transform.position;
        target.TakeHit(new Hit(payload, at, at, Vector2.zero, 0, null));
    }

    // One slot per ship (the default): the strongest ticks, the others wait.
    public override bool IsAtLeastAsStrongAs(Effect other) =>
        other is not DamageOverTimeEffect o || percentMaxHullPerSecond >= o.percentMaxHullPerSecond;

    protected override Effect Stacked(float times)
    {
        var copy = (DamageOverTimeEffect)base.Stacked(times);
        copy.percentMaxHullPerSecond = percentMaxHullPerSecond * times;
        return copy;
    }
}
