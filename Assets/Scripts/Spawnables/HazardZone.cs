using System.Collections.Generic;
using UnityEngine;

// A lingering area left by an explosive shot (Nuke's toxic cloud). Every 0.5 s it hits each enemy inside with a share
// of its max hull, typeless, credited to the weapon that fired. It hurts enemies only and fades out over its life.
// The prefab's sprite is a circle 1 unit across; the zone scales it to its radius.
public class HazardZone : MonoBehaviour
{
    private const float TickInterval = 0.5f;

    [SerializeField] private float duration = 4f;
    [Tooltip("Percent of each enemy's max hull per second.")]
    [SerializeField] private float percentMaxHullPerSecond = 2f;
    [Tooltip("The zone's radius as a share of the blast that left it.")]
    [SerializeField] private float radiusOfBlast = 1f;
    [Tooltip("Seconds the cloud takes to spread from half size and thicken after the blast. Looks only: it hurts at " +
             "its full radius from the start.")]
    [SerializeField] private float spreadTime = 0.6f;

    private HitPayload payload;
    private float radius;
    private int targetMask;
    private float age;
    private float sinceTick;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private readonly HashSet<Ship> hitThisTick = new();

    public void Initialize(in HitPayload shot, float blastRadius, int detectLayer)
    {
        payload = HitPayload.PercentOfMaxHull(percentMaxHullPerSecond * TickInterval, shot.Source, shot.Attacker);
        radius = blastRadius * radiusOfBlast;
        // The shot's detect layer is two above the layer of the ships it hits, as for explosions.
        targetMask = 1 << (detectLayer - 2);
        transform.localScale = Vector3.one * radius * 2f;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= duration)
        {
            Destroy(gameObject);
            return;
        }
        float spread = spreadTime > 0f ? Mathf.SmoothStep(0f, 1f, age / spreadTime) : 1f;
        transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.5f, 1f, spread);
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b,
            baseColor.a * spread * (1f - age / duration));

        sinceTick += Time.deltaTime;
        if (sinceTick < TickInterval)
            return;
        sinceTick -= TickInterval;
        Tick();
    }

    // A ship can have several colliders on its layer: each is hit once per tick.
    private void Tick()
    {
        hitThisTick.Clear();
        foreach (var collider in Physics2D.OverlapCircleAll(transform.position, radius, targetMask))
        {
            Ship ship = collider.GetComponent<Ship>();
            if (ship == null || ship.IsDead || !hitThisTick.Add(ship))
                continue;

            Vector2 at = ship.transform.position;
            ship.TakeHit(new Hit(payload, at, at, Vector2.zero, 0, null));
        }
    }
}
