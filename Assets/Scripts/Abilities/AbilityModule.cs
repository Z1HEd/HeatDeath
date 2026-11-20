using System;
using UnityEngine;

// The ship's one active control (Space, or its HUD button). The button follows its events.
public abstract class AbilityModule : ModuleBase
{
    protected Texture2D icon;

    public event Action<float> OnCooldownChanged;
    public event Action<float> OnDurationChanged;
    public event Action<Texture2D> OnIconChanged;
    public event Action<bool> OnActivatableChanged;

    // The shares of the cooldown and the duration left: 1 just after use or activation, 0 when ready or over.
    public virtual float CooldownLeft => 0f;
    public virtual float DurationLeft => 0f;
    public virtual bool IsActivatable => false;
    public virtual bool IsActive => false;

    public virtual void Activate() {}
    public virtual void Deactivate() {}

    protected void RaiseCooldownChanged() => OnCooldownChanged?.Invoke(CooldownLeft);
    protected void RaiseDurationChanged() => OnDurationChanged?.Invoke(DurationLeft);
    protected void RaiseIconChanged() => OnIconChanged?.Invoke(icon);
    protected void RaiseActivatableChanged() => OnActivatableChanged?.Invoke(IsActivatable);
}
