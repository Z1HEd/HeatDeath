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

    public void Hit(IHitter hitter)
    {
        if (dead || hitter == null)
            return;

        ShipCoreModule core = coreModule;
        if (core == null)
            return;

        bool tookDamage = core.ApplyDamage(hitter);
        bool died = core.CurrentHealth<=0.0;

        if (died){
            Die();
            return;
        }
        if (tookDamage)
        {
            StartCoroutine(DamageFlash());
        }
        ApplyEffects(hitter.GetEffects());
    }

    public void ApplyKnockback(IHitter hitter, Vector3 direction)
    {
        if (dead || IgnoresKnockback)
            return;

        if (shipRigidbody == null || hitter.KnockbackPower <= 0f)
            return;

        if (direction.sqrMagnitude <= 0f)
        {
            direction = Vector2.up;
        }

        shipRigidbody.AddForce(-direction * hitter.KnockbackPower, ForceMode2D.Impulse);
        knockbackTimeRemaining = Mathf.Max(knockbackTimeRemaining, knockbackFreezeDuration);
    }

    public void ApplyKnockback(IHitter hitter, Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);
        Vector3 direction = contact.normal;

        ApplyKnockback(hitter,direction);
    }
    // Each ship gets its own copies: one explosion can hit several ships with the same list.
    public void ApplyEffects(List<Effect> effects)
    {
        if (effects == null)
            return;

        foreach (var effect in effects)
            if (effect != null)
                effectManager.AddStatusEffect(effect.Copy());
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

        var hittable = collision.gameObject.GetComponent<IHittable>();
        if (hittable != null)
        {
            hittable.Hit(core);
            hittable.ApplyKnockback(core, collision);
        }
    }


    public void SetTargetPosition(Vector3 targetPosition)
    {
        foreach (var module in moduleManager.MovementModules)
        {
            module.targetPosition = targetPosition;
        }
    }
}
