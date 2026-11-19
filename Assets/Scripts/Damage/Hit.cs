using System.Collections.Generic;
using UnityEngine;

// One hit on one target, built by whatever hits it: the shot's payload plus what's specific to this target.
public readonly struct Hit
{
    public readonly HitPayload Payload;
    // Where the shot was fired from, for the distance bonus.
    public readonly Vector2 FiredFrom;
    // Where the hit comes from: backstab when it's above the target, and the shield ripple.
    public readonly Vector2 From;
    // Where knockback pushes the target. Its length scales the push (explosions push less toward their edge).
    public readonly Vector2 Push;
    // Enemies the same shot hit before this one: pierced, caught earlier in a blast, or earlier in a Tesla chain.
    public readonly int EnemiesHitBefore;
    // On-hit effects; the target copies each one.
    public readonly List<Effect> Effects;
    // Multiplies the hit's whole damage, bonuses included: a Tesla jump that deals less than the hit before it.
    public readonly float DamageScale;

    public Hit(in HitPayload payload, Vector2 firedFrom, Vector2 from, Vector2 push, int enemiesHitBefore,
        List<Effect> effects, float damageScale = 1f)
    {
        Payload = payload;
        FiredFrom = firedFrom;
        From = from;
        Push = push;
        EnemiesHitBefore = enemiesHitBefore;
        Effects = effects;
        DamageScale = damageScale;
    }
}
