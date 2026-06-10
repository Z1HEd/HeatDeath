using UnityEngine;

public interface IHittable
{
    void Hit(IHitter hitter);
    void ApplyKnockback(IHitter hitter, Collision2D collision);
    void ApplyKnockback(IHitter hitter, Vector3 direction);
}

