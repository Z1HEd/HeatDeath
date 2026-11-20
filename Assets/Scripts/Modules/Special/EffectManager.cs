using System.Collections.Generic;
using UnityEngine;

// A ship's effects come in two ways: effects an owner holds (SetEffects: upgrades, abilities, ship-module features,
// enemy scaling, dev shortcuts), and effects attackers put on the ship (AddHitEffect).
[RequireComponent(typeof(Ship))]
public class EffectManager : MonoBehaviour
{
    [SerializeReference] [SubclassSelector] private List<Effect> activeEffects = new List<Effect>();
    // The on-hit subset of activeEffects, and those of them waiting behind a stronger one in their slot.
    private readonly List<Effect> hitEffects = new List<Effect>();
    private readonly HashSet<Effect> waitingEffects = new HashSet<Effect>();
    // What modules and the appearance get: the active effects that aren't waiting.
    private readonly List<Effect> appliedEffects = new List<Effect>();
    // Effects set with SetEffects, by owner (an upgrade, a module, a buff), and the owner of each.
    private readonly Dictionary<object, List<Effect>> sourcedEffects = new Dictionary<object, List<Effect>>();
    private readonly Dictionary<Effect, object> effectSources = new Dictionary<Effect, object>();
    private Ship ship;
    private bool shouldRecalculate = false;

    // Carries any on-hit effect (from an attacker), waiting ones included: a slow, stun, jam, and so on.
    public bool HasHitEffects => hitEffects.Count > 0;

    private void Awake()
    {
        ship = GetComponent<Ship>();
    }

    private void Update()
    {
        for (int i = 0; i < activeEffects.Count;)
        {
            // A waiting effect's time runs, but it doesn't act: a waiting DOT doesn't tick.
            Effect effect = activeEffects[i];
            bool running = waitingEffects.Contains(effect)
                ? effect.Elapse(Time.deltaTime)
                : effect.Update(Time.deltaTime);
            if (running)
                i++;
            else
                RemoveEffect(effect);
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
        appliedEffects.Clear();
        foreach (var effect in activeEffects)
            if (!waitingEffects.Contains(effect))
                appliedEffects.Add(effect);

        foreach (var module in ship.ModuleManager.GetModules<ModuleBase>())
            module.UpdateEffects(appliedEffects);
        ship.Appearance.Rebuild(appliedEffects);
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

    // On-hit effects. A repeat of one the ship has (a copy of the same asset effect at the same strength) only extends
    // it, so it keeps running: a DOT keeps its ticks. Effects in the same slot (SharesSlotWith) don't stack: the
    // strongest applies, the first of equals, and the others wait with their time running and take over when it
    // ends. Effects that owners hold stack: see SetEffects.
    public void AddHitEffect(Effect effect)
    {
        foreach (var current in hitEffects)
        {
            if (current.Template == effect.Template && IsEquallyStrong(current, effect))
            {
                current.ExtendTo(effect.TimeLeft);
                return;
            }
        }

        hitEffects.Add(effect);
        AddEffect(effect);
        UpdateWaiting();
    }

    // An on-hit effect waits while another in its slot outranks it: a stronger one, or an equal one that came first.
    private void UpdateWaiting()
    {
        waitingEffects.Clear();
        for (int i = 0; i < hitEffects.Count; i++)
        {
            for (int j = 0; j < hitEffects.Count; j++)
            {
                Effect effect = hitEffects[i], rival = hitEffects[j];
                if (i != j && effect.SharesSlotWith(rival) && rival.IsAtLeastAsStrongAs(effect)
                    && (j < i || !effect.IsAtLeastAsStrongAs(rival)))
                {
                    waitingEffects.Add(effect);
                    break;
                }
            }
        }
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

    private static bool IsEquallyStrong(Effect a, Effect b) => a.IsAtLeastAsStrongAs(b) && b.IsAtLeastAsStrongAs(a);

    private static bool IsSameCopy(Effect a, Effect b) => a.Duration == b.Duration && IsEquallyStrong(a, b);

    private void RemoveEffect(Effect effect)
    {
        if (hitEffects.Remove(effect))
            UpdateWaiting();
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
