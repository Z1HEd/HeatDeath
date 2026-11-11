using System.Collections.Generic;
using UnityEngine;

// What effects do to a ship's look and layer. EffectManager rebuilds it from every active effect on
// each recalculation, like stats, so overlapping effects combine and none leaves a stale state behind.
// Tints multiply together. The ship is on the Ghost layer while any effect makes it a ghost.
// Thruster colours can't combine, so the newest active one wins.
public class ShipAppearance
{
    private readonly Ship ship;
    private readonly int baseLayer;
    private readonly int ghostLayer = LayerMask.NameToLayer("Ghost");
    // Each renderer's colour before any tint, recorded the first time it's rebuilt.
    private readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();

    private Color tint;
    private bool isGhost;

    // The effect whose thruster colour wins, and the one whose colour is showing.
    private object thrusterSource, shownThrusterSource;
    private ParticleSystem.MinMaxGradient thrusterColor;
    private List<ParticleSystem> thrusterOriginals;
    private readonly List<ParticleSystem> thrusterClones = new List<ParticleSystem>();

    public ShipAppearance(Ship ship)
    {
        this.ship = ship;
        baseLayer = ship.gameObject.layer;
    }

    public void MultiplyTint(Color color) => tint *= color;
    public void MakeGhost() => isGhost = true;

    public void SetThrusterColor(object source, ParticleSystem.MinMaxGradient color)
    {
        thrusterSource = source;
        thrusterColor = color;
    }

    public void Rebuild(IEnumerable<Effect> effects)
    {
        tint = Color.white;
        isGhost = false;
        thrusterSource = null;
        foreach (var effect in effects)
            effect.ApplyToAppearance(this);

        ship.gameObject.layer = isGhost ? ghostLayer : baseLayer;
        Tint(ship.GetComponent<SpriteRenderer>());
        foreach (var weapon in ship.moduleManager.WeaponModules)
            Tint(weapon.GetComponent<SpriteRenderer>());

        if (thrusterSource != shownThrusterSource)
            ShowThrusterColor();
    }

    // Colours by emitting from clones of the thruster particles. The old clones stop emitting, and
    // their particles fade out on their own; with no colour left, the originals emit again.
    private void ShowThrusterColor()
    {
        shownThrusterSource = thrusterSource;

        foreach (var clone in thrusterClones)
        {
            if (clone == null)
                continue;
            clone.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Object.Destroy(clone.gameObject, clone.main.startLifetime.constantMax + clone.main.duration + 0.5f);
        }
        thrusterClones.Clear();

        // Collected before the first clone exists, so clones are never cloned.
        if (thrusterOriginals == null)
        {
            thrusterOriginals = new List<ParticleSystem>();
            foreach (var movementModule in ship.moduleManager.MovementModules)
                thrusterOriginals.AddRange(movementModule.GetComponentsInChildren<ParticleSystem>());
        }

        foreach (var original in thrusterOriginals)
        {
            if (thrusterSource == null)
            {
                original.Play();
                continue;
            }

            var clone = Object.Instantiate(original, original.transform.parent);
            clone.transform.SetLocalPositionAndRotation(original.transform.localPosition, original.transform.localRotation);
            clone.transform.localScale = original.transform.localScale;
            var colorOverLifetime = clone.colorOverLifetime;
            colorOverLifetime.color = thrusterColor;

            original.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            clone.Clear();
            clone.Play();
            thrusterClones.Add(clone);
        }
    }

    private void Tint(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
            return;

        if (!baseColors.TryGetValue(spriteRenderer, out Color baseColor))
            baseColors[spriteRenderer] = baseColor = spriteRenderer.color;
        spriteRenderer.color = baseColor * tint;
    }
}
