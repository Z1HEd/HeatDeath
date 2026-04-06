using UnityEngine;

[RequireComponent(typeof(ModuleManager))]
public class Ship : MonoBehaviour
{
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

    void Start()
    {
        moduleManager = GetComponent<ModuleManager>();
        currentCharacteristics = baseCharacteristics;
        health = currentCharacteristics.MaxHealth;
        shields = currentCharacteristics.MaxShields;
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
            return;
        }

        health -= damage;
        if (health <= 0) Die();
    }
    public void Die()
    {
        Debug.Log("Died");
        enabled = false;
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
