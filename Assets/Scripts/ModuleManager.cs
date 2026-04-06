using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class ModuleManager : MonoBehaviour
{
    protected Ship ship;
    [SerializeField]
    public List<MovementModule> movementModules = new List<MovementModule>();

    public void Start()
    {
        ship = GetComponent<Ship>();
    }
    public void AddModule(ModuleBase module)
    {
        if (module is MovementModule && !movementModules.Contains(module as MovementModule))
            movementModules.Add(module as MovementModule);
    }
    public void RemoveModule(ModuleBase module)
    {
        if (module is MovementModule && movementModules.Contains(module as MovementModule))
            movementModules.Remove(module as MovementModule);
    }
}
