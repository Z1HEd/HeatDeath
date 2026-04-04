using UnityEngine;

[RequireComponent(typeof(ModuleManager))]
public class Ship : MonoBehaviour
{
    [SerializeField]
    protected ModuleManager moduleManager;

    void Start()
    {
        moduleManager = GetComponent<ModuleManager>();
        moduleManager.movementModules.Add(gameObject.AddComponent<ThrusterModule>());
    }
}
