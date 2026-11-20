using UnityEngine;

// The 0.5 s ticks of damage over time and hazard zones. A tick falls at the end of each interval, the one on the last
// moment included, so a 4 s effect ticks 8 times.
public struct DamageTicks
{
    public const float Interval = 0.5f;
    // Absorbs float error, so the tick on the last moment isn't lost.
    private const float Tolerance = 1e-3f;

    private float sinceTick;

    // The ticks that fall in the next delta seconds, of which only timeLeft still count.
    public int Advance(float delta, float timeLeft)
    {
        sinceTick += Mathf.Clamp(timeLeft, 0f, delta);
        int due = 0;
        for (; sinceTick >= Interval - Tolerance; due++)
            sinceTick -= Interval;
        return due;
    }
}
