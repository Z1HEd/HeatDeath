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
    public List<Effect> GetEffects() => appliesEffects;

    public const float EXPANSION_DURATION = 0.2f;
    public const float FADE_DURATION = 0.3f;

    private float timer;
    private float targetRadius;
    private int detectLayer;

    private bool expanding = true;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;

    private readonly HashSet<Ship> hitShips = new();

    public void Initialize(IHitter hitter, float radius, int detectLayer)
    {
        Damage = hitter.Damage;
        KnockbackPower = hitter.KnockbackPower;
        HPDamageMultiplier = hitter.HPDamageMultiplier;
        ShieldDamageMultiplier = hitter.ShieldDamageMultiplier;
        BackstabMultiplier = hitter.BackstabMultiplier;
        appliesEffects = hitter.GetEffects();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        this.targetRadius = radius;
        this.detectLayer = detectLayer;

        hitShips.Clear();

        transform.localScale = Vector3.zero;

        timer = 0f;
        expanding = true;

        circleCollider.enabled = true;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (expanding)
        {
            float t = Mathf.Clamp01(timer / EXPANSION_DURATION);

            transform.localScale = Vector3.one * Mathf.Lerp(0f, targetRadius, t);

            // Fully visible during damage phase
            var c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);

            if (t >= 1f)
            {
                expanding = false;
                timer = 0f;

                // stop all damage interactions
                circleCollider.enabled = false;
            }
        }
        else
        {
            float t = Mathf.Clamp01(timer / FADE_DURATION);

            transform.localScale = Vector3.one * targetRadius;

            var c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f - t);

            if (t >= 1f)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!expanding)
            return;

        if (other.gameObject.layer != detectLayer - 2)
            return;

        Ship ship = other.GetComponent<Ship>();

        if (ship == null || ship.IsDead || !hitShips.Add(ship))
            return;

        ship.Hit(this);

        if (KnockbackPower > 0f)
            ship.ApplyKnockback(this, transform.position - ship.transform.position);
    }
}