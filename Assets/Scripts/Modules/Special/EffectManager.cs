using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class EffectManager : MonoBehaviour
{
    [SerializeReference] [SubclassSelector] private List<Effect> activeEffects = new List<Effect>();
    public Ship ship;

    public event Action OnChanged;
    public bool shouldRecalculate = false;

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
    public void AddEffect(Effect effect)
    {
        activeEffects.Add(effect);
        effect.ApplyToShip(ship);
        shouldRecalculate = true;
    }
    public void RemoveEffect(Effect effect)
    {
        activeEffects.Remove(effect);
        effect.RemoveFromShip(ship);
        shouldRecalculate = true;
    }
}
