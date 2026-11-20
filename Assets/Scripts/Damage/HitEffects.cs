using System.Collections.Generic;
using UnityEngine;

// A shot's on-hit effects, fixed when it's fired: the module's own, and the sets upgrades grant, each with its chance.
// Every hit rolls the chances again, so each ship a shot pierces or a blast catches, and each blast of a mine, rolls.
public sealed class HitEffects
{
    private readonly List<Effect> own;
    private readonly List<(float chance, List<Effect> effects)> granted;

    public HitEffects(List<Effect> own, List<(float chance, List<Effect> effects)> granted)
    {
        this.own = own;
        this.granted = granted;
    }

    // One hit's effects: the own ones, and each granted set that passes its roll. The target copies each one.
    public IEnumerable<Effect> Roll()
    {
        foreach (var effect in own)
            yield return effect;
        foreach (var (chance, effects) in granted)
            if (Random.value < chance)
                foreach (var effect in effects)
                    yield return effect;
    }
}
