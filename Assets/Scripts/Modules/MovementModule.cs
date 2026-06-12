using UnityEngine;
using System.Collections.Generic;
public abstract class MovementModule : ModuleBase
{
    [SerializeField] 
    public Vector2 targetPosition;
    [SerializeField] public ScalarStat maxSpeed = new ScalarStat(StatType.MaxSpeed, 10f, 0f);
    public Rigidbody2D body;
    protected override void Awake()
    {
        base.Awake();
        body = GetComponentInParent<Rigidbody2D>();
        targetPosition = body.position;
    }
    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifierAggregate> modifiers)
    {
        maxSpeed.Recalculate(modifiers);
    }
    protected override void ResetModifiers()
    {
        maxSpeed.ResetToBase();
    }
    
}
   