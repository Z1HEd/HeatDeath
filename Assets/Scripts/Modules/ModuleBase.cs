using UnityEngine;

[RequireComponent(typeof(Ship))]
public abstract class ModuleBase: MonoBehaviour
{
    protected Ship ship;
    protected virtual void Start()
    {
        ship = GetComponent<Ship>();
        ship.AddModule(this);
    }

    protected virtual void OnDestroy()
    {
        ship.moduleManager.RemoveModule(this);
    }
}
