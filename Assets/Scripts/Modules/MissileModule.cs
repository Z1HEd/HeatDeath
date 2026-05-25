using UnityEngine;
using UnityEngine.Serialization;

public class MissileModule : ProjectileModule
{
    public Sprite textureWithRocket;
    public Sprite textureWithoutRocket;
    protected SpriteRenderer spriteRenderer;
    protected override void Start()
    {
        base.Start();
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
}