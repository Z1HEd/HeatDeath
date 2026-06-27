using UnityEngine;
using System;
using System.Collections.Generic;

[ExecuteAlways]
public abstract class WeaponModule : ModuleBase
{
    [SerializeField] public Transform firePoint;
    [SerializeField] protected ResourceStat fireCooldown = new ResourceStat(StatType.Cooldown, 1f, 0f);
    [SerializeField] protected ScalarStat timeScale = new ScalarStat(StatType.TimeScale, 1f, 0f);
    [SerializeField] protected ScalarStat range = new ScalarStat(StatType.Range, 15f, 0f);
    
    [SerializeField] public BoolStat canAim = new BoolStat(StatType.CanAim, true);
    
    protected RangeDetector rangeDetector;
    public event Action<float> updateCooldownOverlay;

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

    public virtual bool CanFire => fireCooldown == fireCooldown.MinValue;
    public virtual float CooldownFraction => Mathf.Clamp(1f-(fireCooldown)/fireCooldown.MaxValue,0f,1f);

    protected virtual void Update()
    {
        fireCooldown.Consume(Time.deltaTime, timeScale);
        updateCooldownOverlay?.Invoke(CooldownFraction);
    }

    protected virtual void Fire() { fireCooldown.ResetToMax(); }

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
    protected override void ApplyModifiers()
    {
        base.ApplyModifiers();

        fireCooldown.Recalculate(currentModifiers,true);
        timeScale.Recalculate(currentModifiers);
        range.Recalculate(currentModifiers);
        canAim.Recalculate(currentModifiers);
        UpdateRange(range);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();

        timeScale.ResetToBase();
        range.ResetToBase();
        canAim.ResetToBase();
    }
}

