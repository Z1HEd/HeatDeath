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
    // One instance per distinct visual prefab the active effects reference.
    private readonly HashSet<StatusVisual> wantedVisuals = new HashSet<StatusVisual>();
    private readonly Dictionary<StatusVisual, StatusVisual> shownVisuals = new Dictionary<StatusVisual, StatusVisual>();
    private readonly List<StatusVisual> endedVisuals = new List<StatusVisual>();

    // The effect whose thruster colour wins, and the one whose colour is showing.
    private object thrusterSource, shownThrusterSource;
    private ParticleSystem.MinMaxGradient thrusterColor;
    private List<ParticleSystem> thrusterOriginals;
    private readonly List<ParticleSystem> thrusterClones = new List<ParticleSystem>();
    // The originals and the clones emitting now, and the original each clone was made from.
    private readonly List<ParticleSystem> thrusters = new List<ParticleSystem>();
    private readonly Dictionary<ParticleSystem, ParticleSystem> cloneOriginals = new Dictionary<ParticleSystem, ParticleSystem>();

    public ShipAppearance(Ship ship)
    {
        this.ship = ship;
        baseLayer = ship.gameObject.layer;
    }

    // The thruster particles that can emit: the originals, and the copies showing a colour while one is set. Looks
    // that change the engine fire (ThrusterCut, ThrusterSmoke) work on these.
    public IReadOnlyList<ParticleSystem> Thrusters
    {
        get
        {
            CollectThrusterOriginals();
            return thrusters;
        }
    }

    // The thruster a colour copy was made from, or the thruster itself.
    public ParticleSystem OriginalOf(ParticleSystem thruster) =>
        cloneOriginals.TryGetValue(thruster, out ParticleSystem original) ? original : thruster;

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
        wantedVisuals.Clear();
        foreach (var effect in effects)
        {
            effect.ApplyToAppearance(this);
            if (effect.Visual != null)
                wantedVisuals.Add(effect.Visual);
        }
        ShowVisuals();

        ship.gameObject.layer = isGhost ? ghostLayer : baseLayer;
        Tint(ship.GetComponent<SpriteRenderer>());
        foreach (var weapon in ship.ModuleManager.WeaponModules)
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
        cloneOriginals.Clear();
        CollectThrusterOriginals();

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
            cloneOriginals[clone] = original;
        }

        thrusters.Clear();
        thrusters.AddRange(thrusterOriginals);
        thrusters.AddRange(thrusterClones);
    }

    // Collected before the first clone exists, so clones are never cloned.
    private void CollectThrusterOriginals()
    {
        if (thrusterOriginals != null)
            return;

        thrusterOriginals = new List<ParticleSystem>();
        foreach (var movementModule in ship.ModuleManager.MovementModules)
            thrusterOriginals.AddRange(movementModule.GetComponentsInChildren<ParticleSystem>());
        thrusters.AddRange(thrusterOriginals);
    }

    // A visual no effect references any more fades out and removes itself; a new one is added and fades in.
    private void ShowVisuals()
    {
        endedVisuals.Clear();
        foreach (var shown in shownVisuals)
            if (!wantedVisuals.Contains(shown.Key))
                endedVisuals.Add(shown.Key);
        foreach (var prefab in endedVisuals)
        {
            if (shownVisuals[prefab] != null)
                shownVisuals[prefab].End();
            shownVisuals.Remove(prefab);
        }

        foreach (var prefab in wantedVisuals)
            if (!shownVisuals.ContainsKey(prefab))
                shownVisuals[prefab] = Object.Instantiate(prefab, ship.transform);
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
