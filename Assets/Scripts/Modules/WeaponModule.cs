using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Ship))]
public abstract class WeaponModule : ModuleBase
{
    [SerializeField] protected ScalarStat fireRate = new ScalarStat(StatType.FireRate, 1f, 0.0001f);
    protected float lastFireTime;

    protected int DetectLayer { get; private set; }

    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        fireRate.Recalculate(modifiers);
    }

    protected override void ResetModifiers()
    {
        fireRate.ResetToBase();
    }

    protected override void Start()
    {
        base.Start();
        DetermineWeaponLayer();
    }

    protected virtual float FireDelay => 1f / fireRate;
    protected virtual bool CanFire => Time.time >= lastFireTime + FireDelay;

    protected virtual void Update() {}

    protected virtual void Fire() { lastFireTime = Time.time; }

    private void DetermineWeaponLayer()
    {
        DetectLayer = GetLayerForShip("DetectPlayer", "DetectEnemy");
    }

    private int GetLayerForShip(string playerLayerName, string enemyLayerName)
    {
        if (ship is Player)
            return LayerMask.NameToLayer(enemyLayerName);

        if (ship is Enemy)
            return LayerMask.NameToLayer(playerLayerName);

        return 0;
    }
}

