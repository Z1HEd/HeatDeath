using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

// What one hit took from a ship.
public readonly struct DamageTaken
{
    // The module the damage came from, and the ship that fired (may be destroyed by now).
    public readonly ModuleDefinition Source;
    public readonly Ship Attacker;
    public readonly float Shields;
    public readonly float Hull;
    public readonly bool ShieldsBroken;
    // This hit took the hull to 0.
    public readonly bool Killed;

    public DamageTaken(ModuleDefinition source, Ship attacker, float shields, float hull, bool shieldsBroken, bool killed)
    {
        Source = source;
        Attacker = attacker;
        Shields = shields;
        Hull = hull;
        ShieldsBroken = shieldsBroken;
        Killed = killed;
    }
}

[RequireComponent(typeof(SpriteRenderer))]
public class ShipCoreModule : ModuleBase
{
    private static readonly int RippleOriginsId = Shader.PropertyToID("_RippleOrigins");
    private static readonly int RippleAgesId = Shader.PropertyToID("_RippleAges");
    // Ripples of recent hits drawn at once, oldest replaced first. Must match MAX_RIPPLES in ShipShield.shader.
    private const int MaxRipples = 4;
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int CoverId = Shader.PropertyToID("_Cover");
    // Purple shields: shield bars and the shield's dark fill take this colour.
    public static readonly Color HardenedShieldColor = new Color(0.32f, 0.02f, 0.55f);
    // Purple shields stand out more than blue ones: a dark shroud that covers the hull, a stronger flash, and a
    // bright rim. The ripples take the rim colour and glow like blue ones, transparent.
    private static readonly Color HardenedRimColor = new Color(0.85f, 0.4f, 1f);
    private const float HardenedFillAlpha = 0.7f;
    private const float DefaultFlashOpacity = 0.65f, HardenedFlashOpacity = 1f;

    [Header("Base Core Stats")]
    [SerializeField] private ResourceStat health = new ResourceStat(StatType.Health, 100f, 1f);
    [SerializeField] private ResourceStat shields = new ResourceStat(StatType.Shields, 100f, 0f);
    [Tooltip("Seconds without taking damage before shields recharge.")]
    [SerializeField] private ScalarStat shieldRegenDelay = new ScalarStat(StatType.ShieldRegenDelay, 3f, 0f);
    [Tooltip("Seconds a recharge takes from empty to full, so more max shields recharge just as fast.")]
    [SerializeField] private ScalarStat shieldRechargeTime = new ScalarStat(StatType.ShieldRechargeTime, 8f, 0.1f);
    [Tooltip("Multiplies the recharge rate. Upgrades raise it by +%, which grows linearly, unlike -% recharge time.")]
    [SerializeField] private ScalarStat shieldRechargeSpeed = new ScalarStat(StatType.ShieldRechargeSpeed, 1f, 0f);
    [Tooltip("Flat shields per second that always regenerate, even under fire.")]
    [SerializeField] private ScalarStat shieldRegen = new ScalarStat(StatType.ShieldRegen, 0f, 0f);
    [Tooltip("Purple shields: while shields are up, knockback and on-hit effects don't apply. Elites, bosses, " +
             "and player modules only, never swarms.")]
    [SerializeField] private BoolStat hardenedShields = new BoolStat(StatType.HardenedShields, false);
    [Tooltip("Disrupted: the shields that are left absorb nothing (all damage goes to the hull) and don't recharge.")]
    [SerializeField] private BoolStat shieldsOffline = new BoolStat(StatType.ShieldsOffline, false);
    [SerializeField] private ScalarStat hpRegen = new ScalarStat(StatType.HPRegen, 0f, 0f);
    [SerializeField] private BoolStat invincibility = new BoolStat(StatType.Invincibility,false);
    [Tooltip("Multiplies all damage this ship takes. Exposed debuffs raise it.")]
    [SerializeField] private ScalarStat damageTakenMultiplier = new ScalarStat(StatType.DamageTakenMultiplier, 1f, 0f);
    [Tooltip("Multiplies all scrap the player gains. Unused on enemies.")]
    [SerializeField] private ScalarStat xpGain = new ScalarStat(StatType.XPGain, 1f, 0f);

    [Header("Ramming")]
    [SerializeField] private ScalarStat damage = new ScalarStat(StatType.Damage, 10f, 0f);
    [SerializeField] private ScalarStat knockbackPower = new ScalarStat(StatType.Knockback, 1f, 0f);
    [SerializeField] private ScalarStat backstabMultiplier = new ScalarStat(StatType.BackstabMultiplier, 1f, 0f);
    [SerializeReference, SubclassSelector] private List<Effect> appliesEffects = new();

    [Header("Shield Visual")]
    [Tooltip("Size of the shield overlay relative to the ship's sprite.")]
    [SerializeField] private float shieldScale = 1.12f;

    protected SpriteRenderer shieldRenderer;
    private Color defaultRimColor, defaultFillColor;
    private readonly Vector4[] rippleOrigins = new Vector4[MaxRipples];
    private readonly float[] rippleStartTimes = new float[MaxRipples];
    private int nextRipple;
    // Game time of the last hit that took shields or hull. Shields wait shieldRegenDelay after it.
    private float lastDamageTime = float.NegativeInfinity;

    public event Action OnHPShieldsChanged;
    // Each hit that takes shields or health. Regen never raises it, and neither do hits while invincible.
    public event Action<DamageTaken> OnDamageTaken;
    // The same for every ship, with the ship that took it.
    public static event Action<Ship, DamageTaken> OnAnyDamageTaken;

    public float CurrentHealth => health.CurrentValue;
    public float CurrentShields => shields.CurrentValue;
    // The shields that count right now: none while they're offline (Disrupted), however many are left.
    // Anything that depends on shields being up reads this, not CurrentShields.
    public float ActiveShields => shieldsOffline ? 0f : shields.CurrentValue;
    public int CurrentMaxHealth => Mathf.RoundToInt(health.MaxValue);
    public int CurrentMaxShields => Mathf.RoundToInt(shields.MaxValue);
    public float HealthFraction => health.Fraction;
    public float MaxHealth => health.MaxValue;
    // Carries any on-hit debuff, for damage bonuses against debuffed targets.
    public bool IsDebuffed => ship.EffectManager.HasHitEffects;
    public float ShieldFraction => shields.Fraction;
    public bool HasHardenedShields => hardenedShields;
    // Purple shields that are up: Ship ignores knockback and on-hit effects.
    public bool BlocksControl => hardenedShields && ActiveShields > 0f;
    // Shields per second right now: the flat regen always, plus the recharge once the delay has passed.
    public float CurrentShieldRegen
    {
        get
        {
            if (shieldsOffline)
                return 0f;
            float rate = shieldRegen;
            if (Time.time - lastDamageTime >= shieldRegenDelay)
                rate += shields.MaxValue / shieldRechargeTime * shieldRechargeSpeed;
            return rate;
        }
    }
    public float CurrentHPRegen => hpRegen.CurrentValue;
    public float XPGain => xpGain;
    public bool IsFull => health.CurrentValue >= health.MaxValue && shields.CurrentValue >= shields.MaxValue;

    // Ramming: this core hits whatever its ship runs into. No damage types, no bonuses, no weapon.
    public Hit RamHit(Vector2 push) => new Hit(
        new HitPayload(damage, 1f, 1f, backstabMultiplier, knockbackPower, default, ModuleDefinition, ship, null),
        transform.position, transform.position, push, 0, EmitHitEffects(appliesEffects));

    protected override void Awake()
    {
        base.Awake();
        shieldRenderer = GetComponent<SpriteRenderer>();

        // The shield is a copy of the ship's sprite, drawn by Custom/ShipShield above ship and guns.
        shieldRenderer.sprite = ship.GetComponent<SpriteRenderer>().sprite;
        // Own copy per ship: with one shared material, shields drew the newest ship's texture.
        shieldRenderer.sharedMaterial = Instantiate(Resources.Load<Material>("ShieldMaterial"));
        defaultRimColor = shieldRenderer.sharedMaterial.GetColor(RimColorId);
        defaultFillColor = shieldRenderer.sharedMaterial.GetColor(FillColorId);
        shieldRenderer.sortingLayerName = "Shields";
        shieldRenderer.sortingOrder = 0;
        transform.localScale = Vector3.one * shieldScale;
        // No ripple has started yet: unused slots draw nothing.
        Array.Fill(rippleStartTimes, float.NegativeInfinity);

        // Only drawn while flashing: the shader is too costly to run invisibly on every ship.
        shieldRenderer.enabled = false;
    }

    // Regeneration. The bars hear about it only when something actually changed: not while a recharge waits out its
    // delay, nor on ships without regen.
    private void Update()
    {
        float shieldsBefore = shields.CurrentValue, healthBefore = health.CurrentValue;
        if (shieldsBefore < shields.MaxValue)
            shields.AddCurrent(CurrentShieldRegen * Time.deltaTime);
        if (healthBefore < health.MaxValue)
            health.AddCurrent(CurrentHPRegen * Time.deltaTime);

        if (shields.CurrentValue != shieldsBefore || health.CurrentValue != healthBefore)
            OnHPShieldsChanged?.Invoke();
    }

    // Takes the hit's damage from shields, then hull. Returns what it took (nothing while invincible).
    public DamageTaken ApplyDamage(in Hit hit)
    {
        if (invincibility) return default;

        HitPayload payload = hit.Payload;
        float amount = payload.Bonuses.Apply(hit, this) * damageTakenMultiplier;
        if (hit.From.y > transform.position.y)
            amount *= payload.BackstabMultiplier;

        float shieldsBefore = shields.CurrentValue, healthBefore = health.CurrentValue;
        bool hadShields = shieldsBefore > 0f;
        if (!shieldsOffline)
            amount = shields.Consume(amount, payload.ShieldMultiplier);
        if (amount > 0f)
            health.Consume(amount, payload.HullMultiplier);

        bool shieldsGone = shields.CurrentValue <= 0f;
        if (shieldsGone)
            HideShield();
        else if (amount <= 0f)
        {
            StopAllCoroutines();
            StartCoroutine(FlashShield(hit.From));
        }

        var taken = new DamageTaken(payload.Source, payload.Attacker, shieldsBefore - shields.CurrentValue,
            healthBefore - health.CurrentValue, hadShields && shieldsGone,
            healthBefore > 0f && health.CurrentValue <= 0f);
        if (taken.Shields > 0f || taken.Hull > 0f)
        {
            lastDamageTime = Time.time;
            OnDamageTaken?.Invoke(taken);
            OnAnyDamageTaken?.Invoke(ship, taken);
        }
        OnHPShieldsChanged?.Invoke();
        return taken;
    }

    private void HideShield()
    {
        StopAllCoroutines();
        shieldRenderer.enabled = false;
    }

    private IEnumerator FlashShield(Vector3 hitPoint)
    {
        const float FlashDuration = 0.7f;
        // Each hit adds a ripple to the next slot. Rapid fire must not replace ripples that are still young, or
        // they'd never get to spread: at this interval, a slot is reused only after about one flash.
        const float MinRippleInterval = FlashDuration / MaxRipples;
        Material shieldMaterial = shieldRenderer.sharedMaterial;
        shieldRenderer.enabled = true;

        float newestRippleStart = rippleStartTimes[(nextRipple + MaxRipples - 1) % MaxRipples];
        if (Time.time - newestRippleStart >= MinRippleInterval)
        {
            rippleStartTimes[nextRipple] = Time.time;
            rippleOrigins[nextRipple] = WorldToSpriteTexels(hitPoint);
            nextRipple = (nextRipple + 1) % MaxRipples;
            shieldMaterial.SetVectorArray(RippleOriginsId, rippleOrigins);
        }

        float flashStartTime = Time.time;
        for (float t = 0f; t < FlashDuration; t = Time.time - flashStartTime)
        {
            float opacity = hardenedShields ? HardenedFlashOpacity : DefaultFlashOpacity;
            shieldRenderer.color = new Color(1, 1, 1, opacity * (1f - t / FlashDuration));
            shieldMaterial.SetVector(RippleAgesId, RippleAges());
            yield return null;
        }

        shieldRenderer.enabled = false;
    }

    // Seconds since each ripple started. Unused slots get a large finite age, which the shader draws as nothing.
    private Vector4 RippleAges()
    {
        var ages = new Vector4();
        for (int i = 0; i < MaxRipples; i++)
            ages[i] = Mathf.Min(Time.time - rippleStartTimes[i], 1000f);
        return ages;
    }

    // Clamped to the sprite, so distant attackers (Tesla, ramming) ripple from the nearest side.
    private Vector2 WorldToSpriteTexels(Vector3 worldPoint)
    {
        Sprite sprite = shieldRenderer.sprite;
        Vector2 fromPivot = (Vector2)transform.InverseTransformPoint(worldPoint) * sprite.pixelsPerUnit;
        return sprite.pivot + Vector2.ClampMagnitude(fromPivot, sprite.rect.width * 0.5f);
    }

    protected override void OnDestroy()
    {
        Destroy(shieldRenderer.sharedMaterial);
        base.OnDestroy();
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(health);
        stats.Add(shields);
        stats.Add(shieldRegen);
        stats.Add(shieldRegenDelay);
        stats.Add(shieldRechargeTime);
        stats.Add(shieldRechargeSpeed);
        stats.Add(hardenedShields);
        stats.Add(shieldsOffline);
        stats.Add(hpRegen);
        stats.Add(invincibility);
        stats.Add(damageTakenMultiplier);
        stats.Add(xpGain);
        stats.Add(damage);
        stats.Add(knockbackPower);
        stats.Add(backstabMultiplier);
    }

    protected override void OnStatsChanged()
    {
        // Added light alone can't show a dark colour, so purple shields' fill also covers the hull (_Cover).
        // Rim and ripples stay added light, so they glow over the dark fill.
        Material shieldMaterial = shieldRenderer.sharedMaterial;
        shieldMaterial.SetFloat(CoverId, hardenedShields ? 1f : 0f);
        shieldMaterial.SetColor(RimColorId, hardenedShields ? WithAlpha(HardenedRimColor, defaultRimColor.a) : defaultRimColor);
        shieldMaterial.SetColor(FillColorId, hardenedShields
            ? WithAlpha(HardenedShieldColor, HardenedFillAlpha) : defaultFillColor);

        OnHPShieldsChanged?.Invoke();
    }

    public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
}
