using System.Collections;
using UnityEngine;

public class LaserProjectile : Projectile
{
    [SerializeField] private float lifeTime = 0.12f;
    [Tooltip("Part of the beam's life spent expanding to full width. The rest narrows and fades.")]
    [SerializeField, Range(0.01f, 0.99f)] private float expandFraction = 0.25f;
    private float lifeTimeLeft = 0f;
    private bool isFired = false;
    [SerializeField] private Color baseColor = new Color(1f,1f,1f);
    private Transform beam;
    private Vector3 fullBeamScale;

    public override void Initialize(Vector2 velocity, ProjectileModule sourceModule)
    {
        base.Initialize(Vector2.zero, sourceModule);
        transform.up = velocity.normalized;
        lifeTimeLeft = lifeTime;
        isFired = true;

        beam = spriteRenderer.transform;
        fullBeamScale = beam.localScale;
        UpdateBeam(0f);
    }

    public override IEnumerator Kill()
    {
        yield break;
    }
    public void Update()
    {
        if (isDead || !isFired) return;

        lifeTimeLeft -= Time.deltaTime;
        if (lifeTimeLeft < 0)
        {
            isDead = true;
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
