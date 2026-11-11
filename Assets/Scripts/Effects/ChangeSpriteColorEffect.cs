using System;
using UnityEngine;

// Tints the ship's hull and guns. Overlapping tints multiply: see ShipAppearance.
[Serializable]
public class ChangeSpriteColorEffect : Effect
{
    [SerializeField]
    private Color color = Color.white;

    public override void ApplyToAppearance(ShipAppearance appearance) => appearance.MultiplyTint(color);
}
