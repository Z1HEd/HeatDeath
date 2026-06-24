using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class MissileModule : ProjectileModule
{
    [SerializeField] public ScalarStat ExplosionRange = new ScalarStat(StatType.ExplosionRange,1f,0f);
    [SerializeField] public ScalarStat SeekingRange = new ScalarStat(StatType.ProjectileRange,1f,0f);
    public Sprite textureWithRocket;
    public Sprite textureWithoutRocket;
    protected SpriteRenderer spriteRenderer;
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Update()
    {
        base.Update();
        if (Time.time - lastFireTime > FireDelay / 2)
            spriteRenderer.sprite = textureWithRocket;
        else
            spriteRenderer.sprite = textureWithoutRocket;
    }

    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        base.ApplyModifiers(modifiers);
        ExplosionRange.Recalculate(modifiers);
        SeekingRange.Recalculate(modifiers);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();
        ExplosionRange.ResetToBase();
        SeekingRange.ResetToBase();
    }
}