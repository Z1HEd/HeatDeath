using UnityEngine;

// A beam that hits every enemy in its line while it lasts: its weapon pierces without limit (-1).
public class LaserProjectile : Projectile
{
    [SerializeField] private float lifeTime = 0.12f;
    [Tooltip("Part of the beam's life spent expanding to full width. The rest narrows and fades.")]
    [SerializeField, Range(0.01f, 0.99f)] private float expandFraction = 0.25f;
    private float lifeTimeLeft = 0f;
    [SerializeField] private Color baseColor = new Color(1f,1f,1f);
    private Transform beam;
    private Vector3 fullBeamScale;

    // The beam doesn't move: it points along the shot's direction, whatever the module's projectile speed.
    public override void Initialize(Vector2 direction, float speed, ProjectileModule sourceModule)
    {
        base.Initialize(direction, 0f, sourceModule);
        transform.up = direction;
        lifeTimeLeft = lifeTime;

        beam = spriteRenderer.transform;
        fullBeamScale = beam.localScale;
        UpdateBeam(0f);
    }

    private void Update()
    {
        if (IsDead) return;

        lifeTimeLeft -= Time.deltaTime;
        if (lifeTimeLeft < 0)
        {
            IsDead = true;
            Destroy(gameObject);
            return;
        }

        UpdateBeam(1f - lifeTimeLeft / lifeTime);
    }

    // Grows to full width at full brightness, then narrows and dims together.
    // Only the visual changes: the collider is on the root and keeps its size.
    private void UpdateBeam(float progress)
    {
        bool expanding = progress < expandFraction;
        float width = expanding
            ? progress / expandFraction
            : 1f - (progress - expandFraction) / (1f - expandFraction);
        float brightness = expanding ? 1f : width;

        beam.localScale = new Vector3(fullBeamScale.x * width, fullBeamScale.y, fullBeamScale.z);
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, brightness);
    }
}
