using System;
using UnityEngine;

[Serializable]
public class ChangeSpriteColorEffect : Effect
{
    [SerializeField]
    private Color color = Color.white;

    private Color storedColor;

    public ChangeSpriteColorEffect() { }

    public ChangeSpriteColorEffect(Color color)
    {
        this.color = color;
    }

    public override bool IsApplicableTo(ModuleDefinition definition)
    {
        return false;
    }

    public override void ApplyToModule(ModuleBase module) { }

    public override void ApplyToShip(Ship ship)
    {
        var spriteRenderer = ship.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        storedColor = spriteRenderer.color;
        spriteRenderer.color = color;
    }

    public override void RemoveFromShip(Ship ship)
    {
        var spriteRenderer = ship.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = storedColor;
    }

    public override Effect Stacked(float times)
    {
        return new ChangeSpriteColorEffect(color);
    }
}