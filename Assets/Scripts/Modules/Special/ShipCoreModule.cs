using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class ShipCoreModule : ModuleBase, IHitter
{
    private static readonly int RippleOriginId = Shader.PropertyToID("_RippleOrigin");
    private static readonly int RippleAgeId = Shader.PropertyToID("_RippleAge");

    [Header("Base Core Stats")]
    [SerializeField] private ResourceStat health = new ResourceStat(StatType.Health, 100f, 1f);
    [SerializeField] private ResourceStat shields = new ResourceStat(StatType.Shields, 100f, 0f);
    [SerializeField] private ScalarStat shieldRegen = new ScalarStat(StatType.ShieldRegen, 2f, 0f);
    [SerializeField] private ScalarStat hpRegen = new ScalarStat(StatType.HPRegen, 0f, 0f);
    [SerializeField] private BoolStat invincibility = new BoolStat(StatType.Invincibility,false);
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
    private float rippleStartTime = float.NegativeInfinity;

    public event Action OnHPShieldsChanged;

    public float CurrentHealth => health.CurrentValue;
    public float CurrentShields => shields.CurrentValue;
    public int CurrentMaxHealth => Mathf.RoundToInt(health.MaxValue);
    public int CurrentMaxShields => Mathf.RoundToInt(shields.MaxValue);
    public float HealthFraction => health.Fraction;
    public float ShieldFraction => shields.Fraction;
    // Health + shields against their maximums, or 0 when both maximums are 0.
    public float DurabilityFraction
    {
        get
        {
            float max = health.MaxValue + shields.MaxValue;
            return max > 0f ? (health.CurrentValue + shields.CurrentValue) / max : 0f;
        }
    }
    public float CurrentShieldRegen => shieldRegen.CurrentValue;
    public float CurrentHPRegen => hpRegen.CurrentValue;
    public float XPGain => xpGain;
    public bool IsFull => health.CurrentValue >= health.MaxValue && shields.CurrentValue >= shields.MaxValue;
    public float Damage => damage;
    public float KnockbackPower => knockbackPower;
    public float ShieldDamageMultiplier => 1f;
    public float HPDamageMultiplier => 1f;
    public float BackstabMultiplier => backstabMultiplier;
    public List<Effect> GetEffects() {return EmitEffects(appliesEffects);}

    protected override void Awake()
    {
        base.Awake();
        shieldRenderer = GetComponent<SpriteRenderer>();

        // The shield is a copy of the ship's sprite, drawn by Custom/ShipShield above ship and guns.
        shieldRenderer.sprite = ship.GetComponent<SpriteRenderer>().sprite;
        // Own copy per ship: with one shared material, shields drew the newest ship's texture.
        shieldRenderer.sharedMaterial = Instantiate(Resources.Load<Material>("ShieldMaterial"));
        shieldRenderer.sortingLayerName = "Shields";
        shieldRenderer.sortingOrder = 0;
        transform.localScale = Vector3.one * shieldScale;

        // Only drawn while flashing: the shader is too costly to run invisibly on every ship.
        shieldRenderer.enabled = false;
    }

    private void Update()
    {
        if (shields.CurrentValue < shields.MaxValue){
            shields.AddCurrent(CurrentShieldRegen * Time.deltaTime);
            OnHPShieldsChanged?.Invoke();
        }

        if (health.CurrentValue < health.MaxValue){
            health.AddCurrent(CurrentHPRegen * Time.deltaTime);
            OnHPShieldsChanged?.Invoke();
        }
        
    }

    public bool ApplyDamage(IHitter hitter)
    {
        float damage = hitter.Damage;
        if (invincibility) return false;
        if (hitter.transform.position.y>transform.position.y) 
            damage *= hitter.BackstabMultiplier;
        
        bool hadShields = shields.CurrentValue > 0f;
        damage = shields.Consume(damage,hitter.ShieldDamageMultiplier);
        if (damage > 0f)
            health.Consume(damage,hitter.HPDamageMultiplier);

        bool shieldsGone = shields.CurrentValue <= 0f;
        if (shieldsGone)
            HideShield();
        else if (damage <= 0f)
        {
            StopAllCoroutines();
            StartCoroutine(FlashShield(hitter.transform.position));
        }

        OnHPShieldsChanged?.Invoke();
        return damage>0 || (hadShields && shieldsGone);
    }

    private void HideShield()
    {
        StopAllCoroutines();
        shieldRenderer.enabled = false;
    }

    protected IEnumerator FlashShield(Vector3 hitPoint)
    {
        const float SHIELD_FLASH_DURATION = 0.7f;
        const float MIN_RIPPLE_INTERVAL = 0.35f;
        Material shieldMaterial = shieldRenderer.sharedMaterial;
        shieldRenderer.enabled = true;

        // Rapid fire must not restart a young ripple, or it would never get to spread.
        if (Time.time - rippleStartTime >= MIN_RIPPLE_INTERVAL)
        {
            rippleStartTime = Time.time;
            shieldMaterial.SetVector(RippleOriginId, WorldToSpriteTexels(hitPoint));
        }

        float flashStartTime = Time.time;
        for (float t = 0f; t < SHIELD_FLASH_DURATION; t = Time.time - flashStartTime)
        {
            shieldRenderer.color = new Color(1,1,1,0.65f * (1f - t / SHIELD_FLASH_DURATION));
            shieldMaterial.SetFloat(RippleAgeId, Time.time - rippleStartTime);
            yield return null;
        }

        shieldRenderer.enabled = false;
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
        stats.Add(hpRegen);
        stats.Add(invincibility);
        stats.Add(xpGain);
        stats.Add(damage);
        stats.Add(knockbackPower);
        stats.Add(backstabMultiplier);
    }

    protected override void OnStatsChanged()
    {
        OnHPShieldsChanged?.Invoke();
    }
}
