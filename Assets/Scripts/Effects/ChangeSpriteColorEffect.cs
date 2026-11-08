using System;
using System.Collections.Generic;
using UnityEngine;

// Tints the ship's hull and guns.
[Serializable]
public class ChangeSpriteColorEffect : Effect
{
    [SerializeField]
    private Color color = Color.white;

    private readonly List<(SpriteRenderer renderer, Color original)> tinted = new();

    public ChangeSpriteColorEffect() { }

    public ChangeSpriteColorEffect(ResourceStat duration, Color color)
    {
        this.duration = duration;
        this.color = color;
    }

    public override bool IsApplicableTo(ModuleDefinition definition)
    {
        return false;
    }

    public override void ApplyToModule(ModuleBase module) { }

    public override void ApplyToShip(Ship ship)
    {
        tinted.Clear();
        Tint(ship.GetComponent<SpriteRenderer>());
        foreach (var weapon in ship.moduleManager.WeaponModules)
            Tint(weapon.GetComponent<SpriteRenderer>());
    }

    private void Tint(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
            return;

        tinted.Add((spriteRenderer, spriteRenderer.color));
        spriteRenderer.color = color;
    }

    public override void RemoveFromShip(Ship ship)
    {
        foreach (var (spriteRenderer, original) in tinted)
            if (spriteRenderer != null)
                spriteRenderer.color = original;

        tinted.Clear();
    }

    public override Effect Stacked(float times)
    {
        var durationCopy = new ResourceStat(duration.Type, duration.BaseValue, duration.MinValue);
        return new ChangeSpriteColorEffect(durationCopy, color);
    }
}