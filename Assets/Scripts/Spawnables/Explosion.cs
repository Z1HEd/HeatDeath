using UnityEngine;
using System.Collections.Generic;

public class Explosion : MonoBehaviour
{
    [Tooltip("Screen shake for a base missile's blast (radius 1.25). Bigger blasts shake harder and longer, in proportion.")]
    [SerializeField]
    public float shakeScreenDuration = 0.25f;
    [Tooltip("Screen shake for a base missile's blast (radius 1.25). Bigger blasts shake harder and longer, in proportion.")]
    [SerializeField] private float shakeStrength = 0.1f;
    private const float ShakeReferenceRadius = 1.25f;
    // From whatever exploded (a missile, a mine, an ability), with where it was fired from, so distance counts from
    // there.
    private HitPayload payload;
    private Vector2 firedFrom;
    private List<Effect> appliesEffects;
    // Share of the payload's damage: a Tesla pulse after the first is weaker.
    private float damageScale;

    public const float EXPANSION_DURATION = 0.2f;
    // Knockback falls from full at the centre to this share at the edge of the blast.
    private const float EDGE_PUSH = 0.5f;
    [SerializeField] private float fadeDuration = 0.3f;

    private float timer;
    private float targetRadius;
    private int detectLayer;

    private bool expanding = true;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;

    private readonly HashSet<Ship> hitShips = new();

    public void Initialize(in HitPayload payload, Vector2 firedFrom, List<Effect> effects, float radius, int detectLayer,
        float damageScale = 1f)
    {
        this.payload = payload;
        this.firedFrom = firedFrom;
        appliesEffects = effects;
        this.damageScale = damageScale;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        this.targetRadius = radius;
        this.detectLayer = detectLayer;

        hitShips.Clear();

        transform.localScale = Vector3.zero;

        timer = 0f;
        expanding = true;

        circleCollider.enabled = true;
        // A blast without shake (the EMP, Shock Field) mustn't strengthen a shake that's already running.
        float shakeScale = BlastRadius / ShakeReferenceRadius;
        if (shakeScreenDuration > 0f)
            ScreenShaker.Instance.Shake(shakeScreenDuration * shakeScale, shakeStrength * shakeScale);
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
            float t = Mathf.Clamp01(timer / fadeDuration);

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

        // hitShips already holds this ship, so it caught one fewer before.
        DamageTaken taken = ship.TakeHit(new Hit(payload, firedFrom, transform.position, PushOn(ship),
            hitShips.Count - 1, appliesEffects, damageScale));
        payload.ReportHit(ship.CoreModule, taken);
    }

    // The blast's full radius in world units, once Initialize has run.
    public float BlastRadius => circleCollider.radius * targetRadius;

    // Away from the centre: full knockback at the centre, EDGE_PUSH of it at the blast's full radius.
    private Vector2 PushOn(Ship ship)
    {
        Vector2 offset = ship.transform.position - transform.position;
        float blastRadius = BlastRadius;
        float towardEdge = blastRadius > 0f ? Mathf.Clamp01(offset.magnitude / blastRadius) : 1f;
        return offset.normalized * Mathf.Lerp(1f, EDGE_PUSH, towardEdge);
    }
}