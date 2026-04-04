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
}
