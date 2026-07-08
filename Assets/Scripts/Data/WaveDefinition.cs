using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveEnemyEntry
{
    public Enemy enemyPrefab;

    [Min(1)]
    public int count = 1;

    [Tooltip("Informational flag - has no effect on spawning behavior itself.")]
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

    [ReadOnly, SerializeField] private float cachedTotalXP;
    [ReadOnly, SerializeField] private int cachedTotalEnemyCount;

    public string WaveName => waveName;
    public IReadOnlyList<WaveEnemyEntry> EnemyEntries => enemyEntries;
    public int MaxConcurrentEnemies => maxConcurrentEnemies;
    public float SpawnCooldown => spawnCooldown;
    public int WaveOrder => waveOrder;

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