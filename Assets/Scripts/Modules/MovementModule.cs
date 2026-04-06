using UnityEngine;

[RequireComponent (typeof(Rigidbody2D))]
public abstract class MovementModule : ModuleBase
{
    [SerializeField] 
    public Vector2 targetPosition;
    protected Rigidbody2D body;
    public void Start()
    {
        body = GetComponent<Rigidbody2D>();
        targetPosition = body.position;
    }
}   