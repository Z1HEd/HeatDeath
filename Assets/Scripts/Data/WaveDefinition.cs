using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveEnemyEntry
{
    public Enemy enemyPrefab;

    [Min(1)]
    public int count = 1;

    [Tooltip("Spawns before the wave's other enemies, which then come as the boss takes damage. " +
             "A sector's last wave needs one.")]
    public bool isBoss = false;
}

[CreateAssetMenu(menuName = "HeatDeath/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [SerializeField] private string waveName;

    [SerializeField] private List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();

    [Min(1)]
    [SerializeField] private int maxConcurrentEnemies = 3;

    [Min(0.01f)]
    [SerializeField] private float spawnCooldown = 3f;

    [Tooltip("Waves with same waveOrder are played in random order")]
    [SerializeField] private int waveOrder = 1;

    [Header("Boss escort (only in waves with a boss)")]
    [Tooltip("Other enemies that may spawn before the boss takes damage.")]
    [Min(0)]
    [SerializeField] private int bossEscortAtStart = 2;

    [Tooltip("The other enemies are released evenly as the boss's health + shields fall, " +
             "all of them by this fraction.")]
    [Range(0f, 0.95f)]
    [SerializeField] private float releaseAllAtBossFraction = 0.25f;

    [Tooltip("How many groups the held-back enemies are released in. Fewer steps give bigger groups.")]
    [Min(1)]
    [SerializeField] private int bossEscortReleaseSteps = 2;

    [ReadOnly, SerializeField] private float cachedTotalXP;
    [ReadOnly, SerializeField] private int cachedTotalEnemyCount;

    public string WaveName => waveName;
    public IReadOnlyList<WaveEnemyEntry> EnemyEntries => enemyEntries;
    public int MaxConcurrentEnemies => maxConcurrentEnemies;
    public float SpawnCooldown => spawnCooldown;
    public int WaveOrder => waveOrder;
    public int BossEscortAtStart => bossEscortAtStart;
    public float ReleaseAllAtBossFraction => releaseAllAtBossFraction;
    public int BossEscortReleaseSteps => Mathf.Max(1, bossEscortReleaseSteps);

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

    public int BossCount
    {
        get
        {
            int total = 0;
            foreach (var entry in enemyEntries)
                if (entry.isBoss)
                    total += Mathf.Max(0, entry.count);
            return total;
        }
    }

    public bool HasBoss => BossCount > 0;

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
    }
#endif
}