using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(ModuleManager))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Ship : MonoBehaviour, IHittable
{
    private static readonly int FlashColorID =
        Shader.PropertyToID("_FlashColor");

    private static readonly int FlashAmountID =
        Shader.PropertyToID("_FlashAmount");
    public Action OnDeath;
    [SerializeField]
    public ModuleManager moduleManager;
    [SerializeField]
    private float knockbackFreezeDuration = 0.15f;

    protected Rigidbody2D shipRigidbody;
    private ShipCoreModule coreModule;
    private float knockbackTimeRemaining;
    protected bool dead;
    protected Material _material;
    protected SpriteRenderer spriteRenderer;

    public bool IsKnockedBack => knockbackTimeRemaining > 0f;
    public bool IsDead => dead;
    public ShipCoreModule CoreModule => coreModule;

    protected virtual void Awake()
    {
        moduleManager = GetComponent<ModuleManager>();
        shipRigidbody = GetComponent<Rigidbody2D>();
        coreModule = GetComponentInChildren<ShipCoreModule>();
    }

    public virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        coreModule?.Initialize();
        _material = Instantiate(spriteRenderer.sharedMaterial);
        spriteRenderer.material = _material;
    }

    public virtual void Update()
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

        bool tookDamage = core.ApplyDamage(hitter.Damage);
        bool died = core.CurrentHealth<=0.0;

        if (died){
            Die();
            return;
        }
        if (tookDamage)
        {
            StartCoroutine(DamageFlash());
        }
    }

    public void ApplyKnockback(IHitter hitter, Collision2D collision)
    {
        if (dead)
            return;

        if (shipRigidbody == null || hitter.KnockbackPower <= 0f)
            return;

        Vector2 direction = Vector2.up;

        if (collision != null)
        {
            ContactPoint2D contact = collision.GetContact(0);
            direction = contact.normal;
            if (direction.sqrMagnitude <= 0f)
            {
                direction = (Vector2)(transform.position - collision.transform.position).normalized;
            }
        }

        if (direction.sqrMagnitude <= 0f)
        {
            direction = Vector2.up;
        }

        shipRigidbody.AddForce(-direction * hitter.KnockbackPower, ForceMode2D.Impulse);
        knockbackTimeRemaining = Mathf.Max(knockbackTimeRemaining, knockbackFreezeDuration);
    }

    public virtual void Die()
    {
        if (dead)
            return;

        dead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
    public virtual IEnumerator DamageFlash()
    {
        _material.SetColor(FlashColorID, Color.white);
        _material.SetFloat(FlashAmountID, 1f);

        yield return new WaitForSeconds(0.03f);

        // red damage fade

        _material.SetColor(
            FlashColorID,
            new Color(1f, 0.2f, 0.2f)
        );

        float duration = 0.12f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float amount = 1f - (t / duration);

            _material.SetFloat(
                FlashAmountID,
                amount
            );

            yield return null;
        }

        _material.SetFloat(FlashAmountID, 0f);
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

    public void AddModule(ModuleBase module)
    {
        if (moduleManager == null)
            moduleManager = GetComponent<ModuleManager>();

        moduleManager.AddModule(module);
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        foreach (var module in moduleManager.MovementModules)
        {
            module.targetPosition = targetPosition;
        }
    }
}
