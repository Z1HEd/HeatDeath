using System;
using System.Collections.Generic;
using UnityEngine;

// What one source (a weapon, ramming, an ability) dealt in a period.
public class SourceStatistics
{
    public float ShieldDamage;
    public float HullDamage;
    public int Kills;

    public float Damage => ShieldDamage + HullDamage;
}

// Counts for one stretch of a run: the whole run, or one wave.
public class StatisticsPeriod
{
    private readonly Dictionary<ModuleDefinition, SourceStatistics> dealtBySource = new();
    private readonly float startTime = Time.time;
    private float endTime = -1f;

    public IReadOnlyDictionary<ModuleDefinition, SourceStatistics> DealtBySource => dealtBySource;
    public float ShieldDamageTaken { get; private set; }
    public float HullDamageTaken { get; private set; }
    // In game time, so drafts and pause don't count. Runs on until the period is closed.
    public float Duration => (endTime >= 0f ? endTime : Time.time) - startTime;

    public void Close() => endTime = Time.time;

    // Damage the player's side dealt. Hits without a source aren't attributed.
    public void RecordDealt(DamageTaken damage)
    {
        ModuleDefinition source = damage.Attacker.Source;
        if (source == null)
            return;

        if (!dealtBySource.TryGetValue(source, out SourceStatistics stats))
            dealtBySource[source] = stats = new SourceStatistics();
        stats.ShieldDamage += damage.Shields;
        stats.HullDamage += damage.Hull;
        if (damage.Killed)
            stats.Kills++;
    }

    public void RecordTaken(DamageTaken damage)
    {
        ShieldDamageTaken += damage.Shields;
        HullDamageTaken += damage.Hull;
    }
}

// Counts what happens in a run, for run statistics and dev tools. GameController owns it.
// More statistics (scrap, levels, ability uses) go on StatisticsPeriod, fed by more events.
public class RunStatistics : IDisposable
{
    public StatisticsPeriod Run { get; } = new StatisticsPeriod();
    // Null between waves.
    public StatisticsPeriod CurrentWave { get; private set; }

    public event Action<StatisticsPeriod> OnWaveEnded;

    public RunStatistics()
    {
        ShipCoreModule.OnAnyDamageTaken += OnDamageTaken;
    }

    public void Dispose()
    {
        ShipCoreModule.OnAnyDamageTaken -= OnDamageTaken;
    }

    public void BeginWave()
    {
        CurrentWave = new StatisticsPeriod();
    }

    public void EndWave()
    {
        if (CurrentWave == null)
            return;

        StatisticsPeriod ended = CurrentWave;
        ended.Close();
        CurrentWave = null;
        OnWaveEnded?.Invoke(ended);
    }

    private void OnDamageTaken(Ship ship, DamageTaken damage)
    {
        Record(Run, ship, damage);
        if (CurrentWave != null)
            Record(CurrentWave, ship, damage);
    }

    private static void Record(StatisticsPeriod period, Ship ship, DamageTaken damage)
    {
        if (ship is Player)
            period.RecordTaken(damage);
        else
            period.RecordDealt(damage);
    }
}
