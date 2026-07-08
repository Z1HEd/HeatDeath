using UnityEngine;
using System.Collections.Generic;
public interface IHittable
{
    bool IsDead {get;}

    void Hit(IHitter hitter);
    void ApplyKnockback(IHitter hitter, Collision2D collision);
    void ApplyKnockback(IHitter hitter, Vector3 direction);
    void ApplyEffects(List<Effect> effects);
}

