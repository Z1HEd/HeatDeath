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
    public event Action OnDeath;

    [SerializeField] private float knockbackFreezeDuration = 0.15f;
    [SerializeField] private List<ParticleSystem> deathParticles = new List<ParticleSystem>();

    private Rigidbody2D shipRigidbody;
    private ShipCoreModule coreModule;
    private float knockbackTimeRemaining;
    private bool dead;
    // The hull's own copy of its material; the guns' copies are made from it.
    private Material hullMaterial;
    // One copy per renderer: sprites with different textures must not share a material.
    private readonly List<Material> flashMaterials = new List<Material>();
    // The running damage flash: a new hit restarts it rather than running a second one alongside.
    private Coroutine flashRoutine;
    private static readonly WaitForSeconds FlashWhiteTime = new WaitForSeconds(0.03f);

    // Set by dev shortcuts only.
    public bool IgnoresKnockback { get; set; }
    public bool IsKnockedBack => knockbackTimeRemaining > 0f;
    // Purple shields are up: no knockback, no on-hit effects.
    private bool BlocksControl => coreModule != null && coreModule.BlocksControl;
    public bool IsDead => dead;
    public ShipCoreModule CoreModule => coreModule;
    public ShipAppearance Appearance { get; private set; }
    public ModuleManager ModuleManager { get; private set; }
    public EffectManager EffectManager { get; private set; }
    public UpgradeManager UpgradeManager { get; private set; }

    protected virtual void Awake()
    {
        ModuleManager = GetComponent<ModuleManager>();
        EffectManager = GetComponent<EffectManager>();
        UpgradeManager = GetComponent<UpgradeManager>();
        shipRigidbody = GetComponent<Rigidbody2D>();
        coreModule = GetComponentInChildren<ShipCoreModule>();

        // In Awake so it exists before any weapon's Start.
        var spriteRenderer = GetComponent<SpriteRenderer>();
        hullMaterial = Instantiate(spriteRenderer.sharedMaterial);
        spriteRenderer.sharedMaterial = hullMaterial;
        flashMaterials.Add(hullMaterial);

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
        if (hullMaterial == null)
            return;

        Material copy = Instantiate(hullMaterial);
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
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DamageFlash());
        }
        ApplyHitEffects(hit.Effects);
        ApplyKnockback(hit.Push, hit.Payload.Knockback);
        return taken;
    }

    private void ApplyKnockback(Vector2 push, float power)
    {
        if (IgnoresKnockback || BlocksControl || power <= 0f)
            return;

        // A hit without a direction pushes down, as before.
        if (push.sqrMagnitude <= 0f)
            push = Vector2.down;

        shipRigidbody.AddForce(push * power, ForceMode2D.Impulse);
        knockbackTimeRemaining = Mathf.Max(knockbackTimeRemaining, knockbackFreezeDuration);
    }

    // Rolled for this hit, and each ship gets its own copies: one explosion can hit several ships with the same effects.
    private void ApplyHitEffects(HitEffects effects)
    {
        if (effects == null || BlocksControl)
            return;

        foreach (var effect in effects.Roll())
            EffectManager.AddHitEffect(effect.Copy());
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
    // White, then a red fade.
    private IEnumerator DamageFlash()
    {
        SetFlash(Color.white, 1f);

        yield return FlashWhiteTime;

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
        if (collision.gameObject.layer == gameObject.layer || coreModule == null)
            return;

        // Pushes the other ship away from the contact.
        var hittable = collision.gameObject.GetComponent<IHittable>();
        if (hittable != null)
            hittable.TakeHit(coreModule.RamHit(-collision.GetContact(0).normal));
    }


    public void SetTargetPosition(Vector2 targetPosition)
    {
        foreach (var module in ModuleManager.MovementModules)
            module.SetTargetPosition(targetPosition);
    }
}
