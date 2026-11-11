using System;
using UnityEngine;

// Recolours the thruster particles. Gradients can't combine, so the newest active one wins: see ShipAppearance.
[Serializable]
public class ChangeThrusterColorEffect : Effect
{
    [SerializeField]
    private ParticleSystem.MinMaxGradient color;

    public override void ApplyToAppearance(ShipAppearance appearance) => appearance.SetThrusterColor(this, color);
}
