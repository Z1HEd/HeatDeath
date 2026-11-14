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
    [Tooltip("Applied to each ship this weapon hits.")]
    [SerializeReference, SubclassSelector] private List<Effect> appliesEffects = new();

    // Fresh copies of the on-hit effects, own and granted by upgrades, for one shot (one target for Tesla).
    public List<Effect> GetHitEffects() => EmitHitEffects(appliesEffects);

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
        firePoint = transform.GetChild(0);
    }

    protected override void Start()
    {
        base.Start();

        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
            ship.AddFlashRenderer(sprite);
    }

    protected bool IsStunned => timeScale <= 0f;
    public virtual bool CanFire => !IsStunned && fireCooldown == fireCooldown.MinValue;
    public virtual float CooldownFraction => Mathf.Clamp01(1f - fireCooldown.Fraction);

    protected virtual void Update()
    {
        fireCooldown.Consume(Time.deltaTime, timeScale);
        updateCooldownOverlay?.Invoke(CooldownFraction);
    }

    protected virtual void Fire() { fireCooldown.ResetToMax(); }

    protected void OnValidate()
    {
        UpdateRange();
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

    private void UpdateRange()
    {
        if (!rangeDetector) return;
        rangeDetector.SetRadius(range);
    }
    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(fireCooldown);
        stats.Add(timeScale);
        stats.Add(range);
        stats.Add(canAim);
    }

    protected override void OnStatsChanged()
    {
        UpdateRange();
    }
}

