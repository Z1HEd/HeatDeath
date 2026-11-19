using System.Collections.Generic;
using UnityEngine;

// A ship's effects come in two ways: effects an owner holds (SetEffects: upgrades, abilities, ship-module features,
// enemy scaling, dev shortcuts), and effects attackers put on the ship (AddHitEffect).
[RequireComponent(typeof(Ship))]
public class EffectManager : MonoBehaviour
{
    [SerializeReference] [SubclassSelector] private List<Effect> activeEffects = new List<Effect>();
    // The on-hit subset of activeEffects, which follows the one-per-slot rule.
    private readonly List<Effect> hitEffects = new List<Effect>();
    // Effects set with SetEffects, by owner (an upgrade, a module, a buff), and the owner of each.
    private readonly Dictionary<object, List<Effect>> sourcedEffects = new Dictionary<object, List<Effect>>();
    private readonly Dictionary<Effect, object> effectSources = new Dictionary<Effect, object>();
    private Ship ship;
    private bool shouldRecalculate = false;

    // Carries any on-hit effect (from an attacker): a slow, stun, jam, and so on.
    public bool HasHitEffects => hitEffects.Count > 0;

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

    private void AddEffect(Effect effect)
    {
        activeEffects.Add(effect);
        effect.ApplyToShip(ship);
        shouldRecalculate = true;
    }

    // On-hit effects: one per slot. A stronger one replaces the old one; an equal one replaces it only if it lasts
    // longer, so a short stun never cuts a longer one short; a weaker one is ignored. Effects that owners hold
    // stack: see SetEffects.
    public void AddHitEffect(Effect effect)
    {
        foreach (var current in hitEffects)
        {
            if (!effect.SharesSlotWith(current))
                continue;

            if (!effect.IsAtLeastAsStrongAs(current))
                return;
            if (current.IsAtLeastAsStrongAs(effect) && current.TimeLeft >= effect.TimeLeft)
                return;

            RemoveEffect(current);
            break;
        }

        hitEffects.Add(effect);
        AddEffect(effect);
    }

    // Makes the owner's effects exactly these copies, so calling it again replaces them instead of adding more.
    // A copy equal to one the owner already has (same asset effect, strength, and duration) restarts the existing
    // one, so nothing is reapplied. A changed copy replaces the old one, and the owner's effects missing from the
    // list are removed.
    public void SetEffects(object source, IReadOnlyList<Effect> effects)
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

    public void ClearEffects(object source)
    {
        if (sourcedEffects.TryGetValue(source, out List<Effect> effects))
            foreach (var effect in new List<Effect>(effects))
                RemoveEffect(effect);
    }

    // Whether any effect the owner set is still running.
    public bool HasEffects(object source) => sourcedEffects.ContainsKey(source);

    private static bool IsSameCopy(Effect a, Effect b) =>
        a.Duration == b.Duration && a.IsAtLeastAsStrongAs(b) && b.IsAtLeastAsStrongAs(a);

    private void RemoveEffect(Effect effect)
    {
        hitEffects.Remove(effect);
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
