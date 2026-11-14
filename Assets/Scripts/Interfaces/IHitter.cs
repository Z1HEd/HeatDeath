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
    // The module the damage comes from, for statistics. A definition, so it outlives the module itself.
    ModuleDefinition Source { get; }
    List<Effect> GetEffects();
}

