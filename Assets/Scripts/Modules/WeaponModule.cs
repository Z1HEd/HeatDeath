using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public abstract class WeaponModule : ModuleBase
{
    [SerializeField] public Transform firePoint;
    [SerializeField] protected ScalarStat fireRate = new ScalarStat(StatType.FireRate, 1f, 0.0001f);
    [SerializeField] protected ScalarStat range = new ScalarStat(StatType.Range, 15f, 0f);
    
    [SerializeField] public BoolStat canAim = new BoolStat(StatType.CanAim, true);
    
    protected RangeDetector rangeDetector;
    protected float lastFireTime;

    protected int DetectLayer { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        
        DetermineWeaponLayer();
        gameObject.layer = DetectLayer;

        EnsureRangeDetector();
        rangeDetector.gameObject.layer = DetectLayer;

        rangeDetector.Initialize(range);
        range.CurrentValueChanged += UpdateRange;
        firePoint = transform.GetChild(0);
    }

    protected virtual float FireDelay => 1f / fireRate;
    protected virtual bool CanFire => Time.time >= lastFireTime + FireDelay;

    protected virtual void Update() {}

    protected virtual void Fire() { lastFireTime = Time.time; }

    protected void OnValidate()
    {
        UpdateRange(range);
    }

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

    protected void EnsureRangeDetector()
    {
        if (rangeDetector == null)
            rangeDetector = GetComponentInChildren<RangeDetector>(true);
        if (rangeDetector == null)
        {
            GameObject detectorObject = new GameObject("RangeDetector");
            detectorObject.transform.SetParent(transform, false);
            rangeDetector = detectorObject.AddComponent<RangeDetector>();
        }
    }

    private void UpdateRange(float newRange)
    {
        if (!rangeDetector) return;
        rangeDetector.SetRadius(range);
    }
    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        fireRate.Recalculate(modifiers);
        range.Recalculate(modifiers);
        canAim.Recalculate(modifiers);
        UpdateRange(range);
    }

    protected override void ResetModifiers()
    {
        fireRate.ResetToBase();
        range.ResetToBase();
        canAim.ResetToBase();
    }
}

