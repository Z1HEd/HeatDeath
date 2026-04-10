using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Ship))]
public abstract class ModuleBase: MonoBehaviour
{
    [SerializeField] private ModuleDefinition moduleDefinition;

    protected struct StatModifier
    {
        public float Flat;
        public float Percent;
    }

    protected Ship ship;
    public ModuleDefinition ModuleDefinition => moduleDefinition;

    protected virtual void Start()
    {
        ship = GetComponent<Ship>();
        ship.AddModule(this);
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
    }

    public virtual void Recalculate()
    {
    }

    protected Dictionary<string, StatModifier> BuildStatModifiers(UpgradeManager upgradeManager)
    {
        var result = new Dictionary<string, StatModifier>();
        if (upgradeManager == null)
            return result;

        IReadOnlyDictionary<UpgradeDefinition, int> allUpgrades = upgradeManager.GetAllUpgrades();
        foreach (var pair in allUpgrades)
        {
            UpgradeDefinition upgrade = pair.Key;
            int stacks = pair.Value;
            if (upgrade == null || stacks <= 0)
                continue;

            IReadOnlyList<UpgradeEffect> effects = upgrade.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                UpgradeEffect effect = effects[i];
                if (effect.stat == null || string.IsNullOrWhiteSpace(effect.stat.Key))
                    continue;

                AddModifier(result, effect.stat.Key, effect.operation, effect.value * stacks);
            }
        }

        return result;
    }

    private void AddModifier(Dictionary<string, StatModifier> map, string statKey, UpgradeEffectOperation operation, float amount)
    {
        if (!map.TryGetValue(statKey, out StatModifier modifier))
            modifier = default;

        if (operation == UpgradeEffectOperation.AddPercent)
            modifier.Percent += amount;
        else
            modifier.Flat += amount;

        map[statKey] = modifier;
    }
}
