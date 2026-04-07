using UnityEngine;
using System;

[RequireComponent(typeof(ModuleManager))]
[RequireComponent(typeof(Rigidbody2D))]
public class Ship : MonoBehaviour, IHittable
{
    public Action OnHPShieldsChanged;
    [SerializeField]
    public ModuleManager moduleManager;
    [SerializeField]
    protected ShipCharacteristics baseCharacteristics;
    [SerializeField]
    protected ShipCharacteristics currentCharacteristics;
    [SerializeField]
    protected float health;
    [SerializeField]
    protected float shields;
    [SerializeField]
    protected float shieldRegen;
    [SerializeField]
    private float knockbackFreezeDuration = 0.15f;

    protected Rigidbody2D shipRigidbody;
    private float knockbackTimeRemaining;

    public bool IsKnockedBack => knockbackTimeRemaining > 0f;
    public float CurrentHealth => health;
    public float CurrentShields => shields;
    public ShipCharacteristics CurrentCharacteristics => currentCharacteristics;

    public virtual void Start()
    {
        moduleManager = GetComponent<ModuleManager>();
        currentCharacteristics = baseCharacteristics;
        health = currentCharacteristics.maxHealth;
        shields = currentCharacteristics.maxShields;
        shieldRegen = currentCharacteristics.shieldRegen;
        shipRigidbody = GetComponent<Rigidbody2D>();
        OnHPShieldsChanged?.Invoke();
    }

    public virtual void Update()
    {
        if (shields < currentCharacteristics.maxShields)
        {
            shields = Mathf.Min(shields + shieldRegen * Time.deltaTime, currentCharacteristics.maxShields);
            OnHPShieldsChanged?.Invoke();
        }

        if (knockbackTimeRemaining > 0f)
        {
            knockbackTimeRemaining -= Time.deltaTime;
        }
    }

    public void Hit(IHitter hitter)
    {
        float damage = hitter.Damage;

        if (shields < damage)
        {
            damage -= shields;
            shields = 0;
        }
        else
        {
            shields -= damage;
            damage = 0f;
        }

        if (damage > 0f)
        {
            health -= damage;
            if (health <= 0) Die();
        }
        OnHPShieldsChanged?.Invoke();
    }

    public void ApplyKnockback(IHitter hitter, Collision2D collision)
    {
        if (shipRigidbody == null || hitter.KnockbackPower <= 0f)
            return;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 direction = contact.normal;
        if (direction.sqrMagnitude <= 0f)
        {
            direction = (Vector2)(transform.position - collision.transform.position).normalized;
            if (direction.sqrMagnitude <= 0f)
            {
                direction = Vector2.up;
            }
        }

        shipRigidbody.AddForce(-direction * hitter.KnockbackPower, ForceMode2D.Impulse);
        knockbackTimeRemaining = Mathf.Max(knockbackTimeRemaining, knockbackFreezeDuration);
    }

    public void Die()
    {
        Debug.Log("Died");
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
        foreach (var module in moduleManager.movementModules)
        {
            module.targetPosition = targetPosition;
        }
    }
}
