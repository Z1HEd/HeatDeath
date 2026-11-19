using UnityEngine;
using UnityEngine.Serialization;
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

    // Every weapon's hit. The old names are ProjectileModule's; Tesla's fields already had these names.
    [Header("Damage")]
    [SerializeField, FormerlySerializedAs("projectileDamage")]
    protected ScalarStat damage = new ScalarStat(StatType.Damage, 10f, 0f);
    [SerializeField, FormerlySerializedAs("ShieldDamageMultiplier")]
    protected ScalarStat shieldDamageMultiplier = new ScalarStat(StatType.ShieldDamageMultiplier, 1f, -1f);
    [SerializeField, FormerlySerializedAs("HPDamageMultiplier")]
    protected ScalarStat hpDamageMultiplier = new ScalarStat(StatType.HPDamageMultiplier, 1f, -1f);
    [SerializeField, FormerlySerializedAs("BackstabMultiplier")]
    protected ScalarStat backstabMultiplier = new ScalarStat(StatType.BackstabMultiplier, 1f, 0f);
    [SerializeField, FormerlySerializedAs("projectileKnockback")]
    protected ScalarStat knockback = new ScalarStat(StatType.ProjectileKnockback, 0f, 0f);

    [Header("Damage bonuses (percent)")]
    [Tooltip("Of the target's max hull, added to each hit. Worthless early, grows with enemy scaling.")]
    [SerializeField] protected ScalarStat percentMaxHullDamage = new ScalarStat(StatType.PercentMaxHullDamage, 0f, 0f);
    [Tooltip("Of the target's current shields, added to each hit. With a shield bypass, a shielded target loses hull " +
             "faster the more shields it has.")]
    [SerializeField] protected ScalarStat percentShieldsDamage = new ScalarStat(StatType.PercentShieldsDamage, 0f, 0f);
    [Tooltip("Per unit between where the shot was fired and the target. Negative values fall off with distance.")]
    [SerializeField] protected ScalarStat distanceDamage = new ScalarStat(StatType.DistanceDamage, 0f, -100f);
    [Tooltip("Per enemy the shot hit before this one: pierced, caught earlier in the blast, or earlier in the " +
             "Tesla chain.")]
    [SerializeField] protected ScalarStat multiHitDamage = new ScalarStat(StatType.MultiHitDamage, 0f, 0f);
    [Tooltip("The most enemies hit before that the multi-hit bonus counts. Rounded down.")]
    [SerializeField] protected ScalarStat multiHitMaxStacks = new ScalarStat(StatType.MultiHitMaxStacks, 5f, 0f);
    [Tooltip("At 0 hull, less the more hull the target has left.")]
    [SerializeField] protected ScalarStat executeDamage = new ScalarStat(StatType.ExecuteDamage, 0f, 0f);
    [Tooltip("Against targets carrying any on-hit debuff (slow, stun, jam, exposed, disrupted).")]
    [SerializeField] protected ScalarStat debuffedTargetDamage = new ScalarStat(StatType.DebuffedTargetDamage, 0f, 0f);
    [Tooltip("Focus fire: per hit in a row on the same target, as counted when the shot is fired.")]
    [SerializeField] protected ScalarStat focusDamage = new ScalarStat(StatType.FocusDamage, 0f, 0f);
    [Tooltip("The most hits in a row focus fire counts. Rounded down.")]
    [SerializeField] protected ScalarStat focusMaxStacks = new ScalarStat(StatType.FocusMaxStacks, 5f, 0f);

    [Header("Momentum")]
    [Tooltip("Percent fire rate after this weapon kills, for Momentum Duration seconds. Each kill restarts it.")]
    [SerializeField] protected ScalarStat momentumFireRate = new ScalarStat(StatType.MomentumFireRate, 0f, 0f);
    [SerializeField] protected ScalarStat momentumDuration = new ScalarStat(StatType.MomentumDuration, 3f, 0f);

    // From the hits this weapon's shots landed: the target it last hit, hits in a row on it, and momentum left.
    private ShipCoreModule focusTarget;
    private int focusStacks;
    private float momentumLeft;

    public DamageBonuses Bonuses => new DamageBonuses
    {
        PercentMaxHull = percentMaxHullDamage,
        PercentShields = percentShieldsDamage,
        PerUnit = distanceDamage,
        PerEnemyHit = multiHitDamage,
        MaxEnemiesCounted = Mathf.FloorToInt(multiHitMaxStacks),
        Execute = executeDamage,
        VsDebuffed = debuffedTargetDamage,
        PerFocusStack = focusDamage,
    };

    // What a shot fired now carries, focus fire included: a shot doesn't change while it flies.
    public HitPayload CreatePayload() => new HitPayload(damage, shieldDamageMultiplier, hpDamageMultiplier,
        backstabMultiplier, knockback, Bonuses, ModuleDefinition, ship, this, focusTarget, focusStacks);

    // Every hit this weapon's shots land comes back here (blocked hits take nothing and don't count).
    public virtual void OnHitLanded(ShipCoreModule target, in DamageTaken taken)
    {
        if (taken.Shields <= 0f && taken.Hull <= 0f)
            return;

        int maxStacks = Mathf.FloorToInt(focusMaxStacks);
        focusStacks = target == focusTarget ? Mathf.Min(focusStacks + 1, maxStacks) : Mathf.Min(1, maxStacks);
        focusTarget = target;

        if (taken.Killed)
            momentumLeft = momentumDuration;
    }

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
        // Momentum speeds up the cooldown like TimeScale, so stuns (TimeScale 0) still stop the weapon.
        momentumLeft = Mathf.Max(0f, momentumLeft - Time.deltaTime);
        float momentum = momentumLeft > 0f ? 1f + momentumFireRate / 100f : 1f;
        fireCooldown.Consume(Time.deltaTime, timeScale * momentum);
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
        stats.Add(damage);
        stats.Add(shieldDamageMultiplier);
        stats.Add(hpDamageMultiplier);
        stats.Add(backstabMultiplier);
        stats.Add(knockback);
        stats.Add(percentMaxHullDamage);
        stats.Add(percentShieldsDamage);
        stats.Add(distanceDamage);
        stats.Add(multiHitDamage);
        stats.Add(multiHitMaxStacks);
        stats.Add(executeDamage);
        stats.Add(debuffedTargetDamage);
        stats.Add(focusDamage);
        stats.Add(focusMaxStacks);
        stats.Add(momentumFireRate);
        stats.Add(momentumDuration);
    }

    protected override void OnStatsChanged()
    {
        UpdateRange();
    }
}

