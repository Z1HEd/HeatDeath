using System;
using UnityEngine;
public abstract class AbilityModule : ModuleBase
{
    protected Texture2D icon;
    public virtual void Activate(){}
    public virtual void Deactivate(){}
    public event Action<float> updateCooldown;
    public event Action<float> updateDuration;
    public event Action<Texture2D> updateIcon;
    public event Action<bool> updateActivatable;
    protected void InvokeUpdateCooldown(){updateCooldown?.Invoke(CooldownFraction);}
    protected void InvokeUpdateDuration(){updateDuration?.Invoke(DurationFraction);}
    protected void InvokeUpdateIcon(){updateIcon?.Invoke(icon);}
    protected void InvokeUpdateActivatable(){updateActivatable?.Invoke(IsActivatable);}
    virtual public float CooldownFraction {get {return 1f;}}
    virtual public float DurationFraction {get {return 0f;}}
    public virtual bool IsActivatable{get{return false;}}
    public virtual bool IsActive{get{return false;}}
}