using UnityEngine;

[RequireComponent (typeof(Rigidbody2D))]
public abstract class MovementModule : ModuleBase
{
    [SerializeField] 
    public Vector2 targetPosition;
    protected Rigidbody2D body;
    protected override void Start()
    {
        base.Start();
        body = GetComponent<Rigidbody2D>();
        targetPosition = body.position;
    }
}
   