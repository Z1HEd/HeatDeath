using UnityEngine;
using System.Collections.Generic;
public class Explosion : MonoBehaviour, IHitter
{
    public float Damage { get; private set; }
    public float KnockbackPower { get; private set; }
    public float HPDamageMultiplier { get; private set; }
    public float ShieldDamageMultiplier { get; private set; }
    public float BackstabMultiplier { get; private set; }
    public List<Effect> appliesEffects { get; private set; }
    public List<Effect> GetEffects() {return appliesEffects;}
    public float timer = -1;
    public const float FADE_DURATION = 0.3f;
    private SpriteRenderer spriteRenderer;

    public void Initialize(IHitter hitter, float radius, int detectLayer)
    {
        Damage = hitter.Damage;
        KnockbackPower = hitter.KnockbackPower;
        HPDamageMultiplier = hitter.HPDamageMultiplier;
        ShieldDamageMultiplier=hitter.ShieldDamageMultiplier;
        BackstabMultiplier=hitter.ShieldDamageMultiplier;
        appliesEffects = hitter.GetEffects();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        transform.localScale = new Vector3(radius,radius,radius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, 1<<(detectLayer-2));
        foreach (Collider2D col in hits)
        {
            Ship ship = col.GetComponent<Ship>();
            if (ship == null || ship.IsDead)
                continue;
            
            ship.Hit(this);
            if (KnockbackPower > 0f)
                ship.ApplyKnockback(this, transform.position-ship.transform.position);
        }
        timer = FADE_DURATION;
        
    }
    public void Update()
    {
        if (timer<0) return;
        timer -= Time.deltaTime;

        spriteRenderer.color = new Color(1f,1f,1f,timer/FADE_DURATION);

        if (timer<0)
            Destroy(gameObject);
    }
}
