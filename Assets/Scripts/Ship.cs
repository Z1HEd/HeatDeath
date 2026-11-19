using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ModuleManager))]
[RequireComponent(typeof(EffectManager))]
[RequireComponent(typeof(UpgradeManager))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Ship : MonoBehaviour, IHittable
{
    private static readonly int FlashColorID =
        Shader.PropertyToID("_FlashColor");

    private static readonly int FlashAmountID =
        Shader.PropertyToID("_FlashAmount");
    public Action OnDeath;
    public ModuleManager moduleManager;

    public UpgradeManager upgradeManager; 
    public EffectManager effectManager;
    [SerializeField]
    private float knockbackFreezeDuration = 0.15f;
    [SerializeField]
    public List<ParticleSystem> deathParticles = new List<ParticleSystem>();

    protected Rigidbody2D shipRigidbody;
    private ShipCoreModule coreModule;
    private float knockbackTimeRemaining;
    protected bool dead;
    protected Material _material;
    protected SpriteRenderer spriteRenderer;
    // One copy per renderer: sprites with different textures must not share a material.
    private readonly List<Material> flashMaterials = new List<Material>();

    // Set by dev shortcuts only.
    public bool IgnoresKnockback { get; set; }
    public bool IsKnockedBack => knockbackTimeRemaining > 0f;
    // Purple shields are up: no knockback, no on-hit effects.
    private bool BlocksControl => coreModule != null && coreModule.BlocksControl;
    public bool IsDead => dead;
    public ShipCoreModule CoreModule => coreModule;
    public ShipAppearance Appearance { get; private set; }

    protected virtual void Awake()
    {
        moduleManager = GetComponent<ModuleManager>();
        effectManager = GetComponent<EffectManager>();
        upgradeManager = GetComponent<UpgradeManager>();
        shipRigidbody = GetComponent<Rigidbody2D>();
        coreModule = GetComponentInChildren<ShipCoreModule>();

        // In Awake so it exists before any weapon's Start.
        spriteRenderer = GetComponent<SpriteRenderer>();
        _material = Instantiate(spriteRenderer.sharedMaterial);
        spriteRenderer.sharedMaterial = _material;
        flashMaterials.Add(_material);

        Appearance = new ShipAppearance(this);
    }

    protected virtual void OnDestroy()
    {
        foreach (var material in flashMaterials)
            Destroy(material);
    }

    // Makes a renderer on this ship (a gun) take part in the damage flash.
    public void AddFlashRenderer(SpriteRenderer flashRenderer)
    {
        if (_material == null)
            return;

        Material copy = Instantiate(_material);
        flashRenderer.sharedMaterial = copy;
        flashMaterials.Add(copy);
    }

    protected virtual void Update()
    {
        if (knockbackTimeRemaining > 0f)
        {
            knockbackTimeRemaining -= Time.deltaTime;
        }
    }

    // One hit, always in this order: damage; death; flash; on-hit effects; knockback. Purple shields are checked
    // after the damage, so the hit that breaks them still applies its effects and pushes.
    public DamageTaken TakeHit(in Hit hit)
    {
        if (dead || coreModule == null)
            return default;

        DamageTaken taken = coreModule.ApplyDamage(hit);
        if (coreModule.CurrentHealth <= 0f)
        {
            Die();
            return taken;
        }

        if (taken.Hull > 0f || taken.ShieldsBroken)
            StartCoroutine(DamageFlash());
        ApplyHitEffects(hit.Effects);
        ApplyKnockback(hit.Push, hit.Payload.Knockback);
        return taken;
    }

    private void ApplyKnockback(Vector2 push, float power)
    {
        if (dead || IgnoresKnockback || BlocksControl || shipRigidbody == null || power <= 0f)
            return;

        // A hit without a direction pushes down, as before.
        if (push.sqrMagnitude <= 0f)
            push = Vector2.down;

        shipRigidbody.AddForce(push * power, ForceMode2D.Impulse);
        knockbackTimeRemaining = Mathf.Max(knockbackTimeRemaining, knockbackFreezeDuration);
    }

    // Each ship gets its own copies: one explosion can hit several ships with the same list.
    private void ApplyHitEffects(List<Effect> effects)
    {
        if (effects == null || BlocksControl)
            return;

        foreach (var effect in effects)
            if (effect != null)
                effectManager.AddHitEffect(effect.Copy());
    }

    public virtual void Die()
    {
        if (dead)
            return;

        dead = true;
        OnDeath?.Invoke();
        foreach (var particle in deathParticles)
        {
            Instantiate(particle,transform.position,Quaternion.identity);
        }
        Destroy(gameObject);
    }
    public virtual IEnumerator DamageFlash()
    {
        SetFlash(Color.white, 1f);

        yield return new WaitForSeconds(0.03f);

        // red damage fade
        Color red = new Color(1f, 0.2f, 0.2f);

        float duration = 0.08f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            SetFlash(red, 1f - (t / duration));

            yield return null;
        }

        SetFlash(red, 0f);
    }

    private void SetFlash(Color color, float amount)
    {
        foreach (var material in flashMaterials)
        {
            material.SetColor(FlashColorID, color);
            material.SetFloat(FlashAmountID, amount);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == null || collision.gameObject.layer == gameObject.layer)
            return;

        ShipCoreModule core = coreModule;
        if (core == null)
            return;

        // Pushes the other ship away from the contact.
        var hittable = collision.gameObject.GetComponent<IHittable>();
        if (hittable != null)
            hittable.TakeHit(core.RamHit(-collision.GetContact(0).normal));
    }


    public void SetTargetPosition(Vector3 targetPosition)
    {
        foreach (var module in moduleManager.MovementModules)
        {
            module.targetPosition = targetPosition;
        }
    }
}
