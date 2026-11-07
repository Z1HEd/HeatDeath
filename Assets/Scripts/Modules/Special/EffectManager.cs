using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class EffectManager : MonoBehaviour
{
    [SerializeReference] [SubclassSelector] private List<Effect> activeEffects = new List<Effect>();
    // The on-hit subset of activeEffects, which follows the one-per-slot rule.
    private readonly List<Effect> statusEffects = new List<Effect>();
    public Ship ship;

    public event Action OnChanged;
    private bool shouldRecalculate = false;

    protected void Awake()
    {
        ship = GetComponent<Ship>();
    }

    public void Update()
    {
        for (int i =0;i<activeEffects.Count;)
        {
            if (!activeEffects[i].Update(Time.deltaTime))
            { 
                RemoveEffect(activeEffects[i]);
                shouldRecalculate = true;
            }
            else i++;

        }
        if (shouldRecalculate){
            ReapplyEffects();
            OnChanged?.Invoke();
            shouldRecalculate = false;
        }
    }

    private void ReapplyEffects()
    {
        ModuleManager moduleManager = GetComponent<ModuleManager>();

        foreach (var module in moduleManager.GetModules<ModuleBase>())
            module.UpdateEffects(activeEffects);
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
    // the timer; a weaker one is ignored. Upgrades and abilities use AddEffect and always stack.
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

    public void RemoveEffect(Effect effect)
    {
        statusEffects.Remove(effect);
        if (!activeEffects.Remove(effect))
            return;

        effect.RemoveFromShip(ship);
        shouldRecalculate = true;
    }
}
