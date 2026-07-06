using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One entry in a wave's enemy pool: which enemy, how many of it spawn
/// during the wave, and whether it's the wave's boss.
/// </summary>
[System.Serializable]
public class WaveEnemyEntry
{
    public Enemy enemyPrefab;

    [Min(1)]
    public int count = 1;

    [Tooltip("Mark true for the boss of a boss wave. Purely informational " +
             "for validation/inspector clarity - spawning treats all entries the same.")]
    public bool isBoss = false;
}

/// <summary>
/// Defines a single wave: which enemies can spawn, how many total, how many
/// can be alive at once, and how often they spawn. Waves are strung together
/// inside a SectorData asset.
/// </summary>
[CreateAssetMenu(menuName = "HeatDeath/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [SerializeField] private string waveName;

    [Tooltip("Enemies that can appear in this wave, and how many of each " +
             "will spawn in total before the wave is considered 'spawned out'. " +
             "The wave only ends once all of these have spawned AND died.")]
    [SerializeField] private List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();

    [Tooltip("Max enemies from this wave allowed alive at the same time.")]
    [Min(1)]
    [SerializeField] private int maxConcurrentEnemies = 3;

    [Tooltip("Seconds between spawn attempts during this wave.")]
    [Min(0.01f)]
    [SerializeField] private float spawnCooldown = 3f;

    [Tooltip("Mark the last wave of a sector as the boss wave. It can still " +
             "contain regular enemies alongside the boss entry/entries.")]
    [SerializeField] private bool isBossWave = false;

    // Read-only in the inspector - always derived from enemyEntries, never
    // hand-edited. Recomputed in OnValidate whenever the wave asset changes.
    [ReadOnly, SerializeField] private float cachedTotalXP;
    [ReadOnly, SerializeField] private int cachedTotalEnemyCount;

    public string WaveName => waveName;
    public IReadOnlyList<WaveEnemyEntry> EnemyEntries => enemyEntries;
    public int MaxConcurrentEnemies => maxConcurrentEnemies;
    public float SpawnCooldown => spawnCooldown;
    public bool IsBossWave => isBossWave;

    /// <summary>Total number of enemies that will spawn over the life of this wave.</summary>
    public int TotalEnemyCount
    {
        get
        {
            int total = 0;
            foreach (var entry in enemyEntries)
                total += Mathf.Max(0, entry.count);
            return total;
        }
    }

    /// <summary>
    /// Sum of XP across every enemy this wave will spawn. This is what gets
    /// added to the run's XP counter once the wave is cleared, and is used
    /// (via the counter) to scale enemy stats for later waves.
    /// </summary>
    public float TotalXP
    {
        get
        {
            float total = 0;
            foreach (var entry in enemyEntries)
            {
                if (entry.enemyPrefab == null) continue;
                total += entry.enemyPrefab.XPReward * Mathf.Max(0, entry.count);
            }
            return total;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cachedTotalXP = TotalXP;
        cachedTotalEnemyCount = TotalEnemyCount;

        if (isBossWave)
        {
            bool hasBoss = enemyEntries.Exists(e => e.isBoss);
            if (!hasBoss)
                Debug.LogWarning($"[WaveDefinition] '{name}' is marked as a boss wave but has no entry flagged isBoss.", this);
        }
    }
#endif
}