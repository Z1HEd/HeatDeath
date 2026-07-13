using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChangeThrusterColorEffect : Effect
{
    [SerializeField]
    private ParticleSystem.MinMaxGradient color;

    // original -> colored clone
    private readonly Dictionary<ParticleSystem, ParticleSystem> clones = new();

    public ChangeThrusterColorEffect() { }

    public ChangeThrusterColorEffect(ResourceStat duration, ParticleSystem.MinMaxGradient color)
    {
        this.duration = duration;
        this.color = color;
    }

    public override bool IsApplicableTo(ModuleDefinition definition) => false;

    public override void ApplyToModule(ModuleBase module) { }

    public override void ApplyToShip(Ship ship)
    {
        clones.Clear();

        foreach (var movementModule in ship.moduleManager.MovementModules)
        {
            foreach (var original in movementModule.GetComponentsInChildren<ParticleSystem>())
            {
                // Create a duplicate beside the original.
                var clone = UnityEngine.Object.Instantiate(
                    original,
                    original.transform.parent);

                clone.transform.SetLocalPositionAndRotation(
                    original.transform.localPosition,
                    original.transform.localRotation);
                clone.transform.localScale = original.transform.localScale;

                // Change only the clone's gradient.
                var col = clone.colorOverLifetime;
                col.color = color;

                // Stop the original from emitting.
                original.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                // Make sure the clone starts emitting.
                clone.Clear();
                clone.Play();

                clones.Add(original, clone);
            }
        }
    }

    public override void RemoveFromShip(Ship ship)
    {
        foreach (var pair in clones)
        {
            var original = pair.Key;
            var clone = pair.Value;

            // Stop colored version. Existing particles continue until they die.
            clone.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // Resume original emission.
            original.Play();

            // Destroy clone after all of its particles have died.
            UnityEngine.Object.Destroy(
                clone.gameObject,
                clone.main.startLifetime.constantMax + clone.main.duration + 0.5f);
        }

        clones.Clear();
    }

    public override Effect Stacked(float times)
    {
        var durationCopy = new ResourceStat(duration.Type, duration.BaseValue, duration.MinValue);
        return new ChangeThrusterColorEffect(durationCopy, color);
    }
}