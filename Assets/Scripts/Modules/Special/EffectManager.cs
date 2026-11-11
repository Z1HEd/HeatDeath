using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class EffectManager : MonoBehaviour
{
    [SerializeReference] [SubclassSelector] private List<Effect> activeEffects = new List<Effect>();
    // The on-hit subset of activeEffects, which follows the one-per-slot rule.
    private readonly List<Effect> statusEffects = new List<Effect>();
    // Effects added with Apply, by source (an upgrade, a module, a buff), and the source of each.
    private readonly Dictionary<object, List<Effect>> sourcedEffects = new Dictionary<object, List<Effect>>();
    private readonly Dictionary<Effect, object> effectSources = new Dictionary<Effect, object>();
    private Ship ship;
    private bool shouldRecalculate = false;

    protected void Awake()
    {
        ship = GetComponent<Ship>();
    }

    public void Update()
    {
        for (int i = 0; i < activeEffects.Count;)
        {
            if (!activeEffects[i].Update(Time.deltaTime))
                RemoveEffect(activeEffects[i]);
            else
                i++;
        }

        if (shouldRecalculate)
        {
            // Cleared first: effects added while reapplying (a module granting effects) need another pass.
            shouldRecalculate = false;
            ReapplyEffects();
        }
    }

    private void ReapplyEffects()
    {
        foreach (var module in ship.moduleManager.GetModules<ModuleBase>())
            module.UpdateEffects(activeEffects);
        ship.Appearance.Rebuild(activeEffects);
    }
    public void MarkDirty()
    {
        shouldRecalculate = true;
    }

    public void AddEffect(Effect effect)
    {
        activeEffects.Add(effect);
        effect.ApplyToShip(ship);
        shouldRecalculate = true;
    }
    // On-hit effects: one per slot. A stronger or equal one replaces the old one and restarts
    // the timer; a weaker one is ignored. Effects from other sources stack: see Apply and AddEffect.
    public void AddStatusEffect(Effect effect)
    {
        foreach (var current in statusEffects)
        {
            if (!effect.SharesSlotWith(current))
                continue;

            if (!effect.IsAtLeastAsStrongAs(current))
                return;

            RemoveEffect(current);
            break;
        }

        statusEffects.Add(effect);
        AddEffect(effect);
    }

    // Makes the source's effects exactly these copies. A copy equal to one the source already has
    // (same asset effect, strength, and duration) restarts the existing one, so nothing is reapplied.
    // A changed copy replaces the old one, and the source's effects missing from the list are removed.
    public void Apply(object source, IReadOnlyList<Effect> effects)
    {
        var previous = sourcedEffects.TryGetValue(source, out List<Effect> current)
            ? new List<Effect>(current)
            : new List<Effect>();

        foreach (var effect in effects)
        {
            Effect old = previous.Find(e => e.Template == effect.Template);
            if (old != null)
            {
                previous.Remove(old);
                if (IsSameCopy(old, effect))
                {
                    old.Restart();
                    continue;
                }

                RemoveEffect(old);
            }

            if (!sourcedEffects.TryGetValue(source, out List<Effect> list))
                sourcedEffects[source] = list = new List<Effect>();
            list.Add(effect);
            effectSources[effect] = source;
            AddEffect(effect);
        }

        foreach (var leftover in previous)
            RemoveEffect(leftover);
    }

    public void Remove(object source)
    {
        if (sourcedEffects.TryGetValue(source, out List<Effect> effects))
            foreach (var effect in new List<Effect>(effects))
                RemoveEffect(effect);
    }

    // Whether any effect the source applied is still running.
    public bool IsActive(object source) => sourcedEffects.ContainsKey(source);

    private static bool IsSameCopy(Effect a, Effect b) =>
        a.Duration == b.Duration && a.IsAtLeastAsStrongAs(b) && b.IsAtLeastAsStrongAs(a);

    public void RemoveEffect(Effect effect)
    {
        statusEffects.Remove(effect);
        if (effectSources.Remove(effect, out object source))
        {
            List<Effect> list = sourcedEffects[source];
            list.Remove(effect);
            if (list.Count == 0)
                sourcedEffects.Remove(source);
        }

        if (!activeEffects.Remove(effect))
            return;

        effect.RemoveFromShip(ship);
        shouldRecalculate = true;
    }
}
