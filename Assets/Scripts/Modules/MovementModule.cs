using UnityEngine;

public abstract class MovementModule : ModuleBase
{
    [SerializeField] 
    public Vector2 targetPosition;
    protected Rigidbody2D body;
    protected override void Start()
    {
        base.Start();
        body = GetComponentInParent<Rigidbody2D>();
        targetPosition = body.position;
    }
}
   