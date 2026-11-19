public interface IHittable
{
    bool IsDead {get;}

    // Damage, on-hit effects, and knockback, in that order. Returns what the hit took.
    DamageTaken TakeHit(in Hit hit);
}
