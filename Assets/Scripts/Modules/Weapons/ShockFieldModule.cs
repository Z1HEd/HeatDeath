using System.Collections.Generic;
using UnityEngine;

// Shock Field: a blast around the ship that reaches the weapon's range and hits everything in it. Chain targets
// beyond the first add more blasts, each weaker. Only the first applies on-hit effects, so they add damage, not stun.
public class ShockFieldModule : TeslaModule
{
    [Header("Shock Field")]
    [SerializeField] private Explosion blastPrefab;
    [Tooltip("Seconds between the pulses of one strike.")]
    [SerializeField] private float pulseInterval = 0.3f;

    protected override void Strike(Ship target, in HitPayload payload)
    {
        if (blastPrefab == null)
            return;

        HitPayload shot = payload;
        StartCoroutine(RepeatStrikes(pulseInterval, (pulse, damageScale) =>
        {
            Blast(shot, pulse == 0 ? GetHitEffects() : null, damageScale);
            return true;
        }));
    }

    // Around the ship wherever it is by then, sized so the blast's edge reaches the weapon's range.
    private void Blast(in HitPayload payload, List<Effect> effects, float damageScale)
    {
        float prefabRadius = blastPrefab.GetComponent<CircleCollider2D>().radius;
        Explosion blast = Instantiate(blastPrefab, transform.position, Quaternion.identity);
        blast.Initialize(payload, transform.position, effects, range / prefabRadius, DetectLayer, damageScale);
    }
}
