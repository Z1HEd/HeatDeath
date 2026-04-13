using UnityEngine;
using System;

[RequireComponent(typeof(ModuleManager))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ShipCoreModule))]
public class Ship : MonoBehaviour, IHittable
{
    public Action OnDeath;
    [SerializeField]
    public ModuleManager moduleManager;
    [SerializeField]
    private float knockbackFreezeDuration = 0.15f;

    protected Rigidbody2D shipRigidbody;
    private ShipCoreModule coreModule;
    private float knockbackTimeRemaining;
    protected bool dead;

    public bool IsKnockedBack => knockbackTimeRemaining > 0f;
    public bool IsDead => dead;

    public virtual void Start()
    {
        moduleManager = GetComponent<ModuleManager>();
        coreModule = GetComponent<ShipCoreModule>();

        coreModule.Initialize();
        shipRigidbody = GetComponent<Rigidbody2D>();
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

        bool isDead = coreModule.ApplyDamage(hitter.Damage);
        if (isDead)
            Die();
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
