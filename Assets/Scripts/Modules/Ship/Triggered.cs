using System;
using System.Collections.Generic;
using UnityEngine;

// Runs its responses when its trigger fires, but only while the cooldown is ready. Each trigger restarts
// the cooldown, so a cooldown shorter than a timed buff lets the buff refresh before it ends.
[Serializable]
public class Triggered : ShipModuleFeature
{
    [SerializeReference, SubclassSelector] private ModuleTrigger trigger;
    [SerializeField] private ResourceStat cooldown = new(StatType.Cooldown, 10f, 0f);
    [SerializeReference, SubclassSelector] private List<TriggerResponse> responses = new();

    private ShipModule module;

    public override void OnStart(ShipModule module)
    {
        this.module = module;
        cooldown.Consume(cooldown.MaxValue); // Starts ready.
        trigger?.Bind(module.Ship, OnTriggered);
    }

    public override void Tick(ShipModule module, float deltaTime) => cooldown.Consume(deltaTime);

    private void OnTriggered()
    {
        if (cooldown.CurrentValue > 0f)
            return;

        cooldown.ResetToMax();
        foreach (var response in responses)
            response?.Run(module);
    }

    public override void OnMultiplierChanged(ShipModule module)
    {
        foreach (var response in responses)
            response?.OnMultiplierChanged(module);
    }

    public override void OnDestroy(ShipModule module)
    {
        trigger?.Unbind(module.Ship);
        foreach (var response in responses)
            response?.OnDestroy(module);
    }

    public override void CollectStats(List<IStat> stats) => stats.Add(cooldown);
}
