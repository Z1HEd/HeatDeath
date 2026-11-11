using System;
using System.Collections.Generic;
using UnityEngine;

// What sets off a triggered ship-module feature, picked per module with SubclassSelector.
[Serializable]
public abstract class ModuleTrigger
{
    private Action onTriggered;

    public void Bind(Ship ship, Action onTriggered)
    {
        this.onTriggered = onTriggered;
        Subscribe(ship);
    }

    public void Unbind(Ship ship)
    {
        Unsubscribe(ship);
        onTriggered = null;
    }

    protected void Fire() => onTriggered?.Invoke();
    protected abstract void Subscribe(Ship ship);
    protected abstract void Unsubscribe(Ship ship);
}

// Fires on hits to the ship that match.
[Serializable]
public abstract class DamageTrigger : ModuleTrigger
{
    protected override void Subscribe(Ship ship) => ship.CoreModule.OnDamageTaken += OnDamageTaken;
    protected override void Unsubscribe(Ship ship) => ship.CoreModule.OnDamageTaken -= OnDamageTaken;

    private void OnDamageTaken(DamageTaken damage)
    {
        if (Matches(damage))
            Fire();
    }

    protected abstract bool Matches(DamageTaken damage);
}

[Serializable]
public class HullDamageTrigger : DamageTrigger
{
    protected override bool Matches(DamageTaken damage) => damage.Hull > 0f;
}

[Serializable]
public class ShieldsBrokenTrigger : DamageTrigger
{
    protected override bool Matches(DamageTaken damage) => damage.ShieldsBroken;
}

// A number of kills within a number of seconds. Kills count during the feature's cooldown too, so once
// the cooldown is shorter than the window, recent kills set it off again and the buff can stay up.
[Serializable]
public class KillStreakTrigger : ModuleTrigger
{
    [SerializeField, Min(1)] private int kills = 5;
    [SerializeField, Min(0f)] private float seconds = 4f;

    private Queue<float> killTimes;

    protected override void Subscribe(Ship ship)
    {
        killTimes = new Queue<float>();
        Enemy.OnAnyKilled += OnKill;
    }

    protected override void Unsubscribe(Ship ship) => Enemy.OnAnyKilled -= OnKill;

    private void OnKill(Enemy enemy)
    {
        killTimes.Enqueue(Time.time);
        while (killTimes.Peek() < Time.time - seconds)
            killTimes.Dequeue();

        if (killTimes.Count >= kills)
            Fire();
    }
}
