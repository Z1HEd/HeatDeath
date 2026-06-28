using UnityEngine;
using System.Collections.Generic;
public interface IHitter
{
    Transform transform {get;}
    float Damage { get; }
    float ShieldDamageMultiplier {get;}
    float HPDamageMultiplier {get;}
    float BackstabMultiplier {get;}
    float KnockbackPower { get; }
    List<Effect> GetEffects();
}

