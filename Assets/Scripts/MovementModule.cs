using System.Reflection;
using UnityEngine;

public abstract class MovementModule : ModuleBase
{
    [SerializeField] public Vector2 targetPosition;
    protected Rigidbody2D body;
    public override void Start()
    {
        base.Start();
        body = GetComponent<Rigidbody2D>();
        targetPosition = body.position;
    }
}   