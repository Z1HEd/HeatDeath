using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public abstract class ModuleBase: MonoBehaviour
{
    protected Ship ship;
    public virtual void Start()
    {
        ship = GetComponent<Ship>();
    }
    public virtual void FixedUpdate() {}
    public virtual void Update(){}
}
