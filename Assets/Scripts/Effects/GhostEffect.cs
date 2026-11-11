using System;

// Moves the ship to the Ghost layer, which collides with nothing and isn't targeted.
[Serializable]
public class GhostEffect : Effect
{
    public override void ApplyToAppearance(ShipAppearance appearance) => appearance.MakeGhost();
}
