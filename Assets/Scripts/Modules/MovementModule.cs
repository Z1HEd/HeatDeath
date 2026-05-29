using UnityEngine;

public abstract class MovementModule : ModuleBase
{
    [SerializeField] 
    public Vector2 targetPosition;
    protected Rigidbody2D body;
    protected override void Awake()
    {
        base.Awake();
        body = GetComponentInParent<Rigidbody2D>();
        targetPosition = body.position;
    }
}
   