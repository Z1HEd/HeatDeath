using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class EffectManager : MonoBehaviour
{
    [SerializeField]
    private List<Effect> activeEffects = new List<Effect>();

    public event Action OnChanged;
    private bool shouldRecalculate = false;

    public void Update()
    {
        if (shouldRecalculate){
            RecalculateAllModules();
            OnChanged?.Invoke();
            shouldRecalculate = false;
        }
    }

    public void RecalculateAllModules()
    {
        ModuleManager moduleManager = GetComponent<ModuleManager>();

        foreach (var module in moduleManager.GetModules<ModuleBase>())
            module.UpdateEffects(activeEffects);
    }
    public void AddEffect(Effect effect)
    {
        activeEffects.Add(effect);
        shouldRecalculate = true;
    }
    public void RemoveEffect(Effect effect)
    {
        activeEffects.Remove(effect);
        shouldRecalculate = true;
    }
}
