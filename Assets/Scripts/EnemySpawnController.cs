using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawnController : MonoBehaviour
{
    private static EnemySpawnController instance;
    public const float ENEMY_SCALING_COEFFICIENT = 1.5f;
    public const float XP_PER_LEVEL = 100f;

    [Header("Progression")]
    [Tooltip("Sectors play in this order (or shuffled, see below) without repeating. " +
             "Once all have been cleared, endless mode begins and sectors may repeat.")]
    [SerializeField] private List<SectorDefinition> sectors;

    [SerializeField] private bool randomizeSectorOrder = true;
    [SerializeField] private bool randomizeEndlessOrder = true;

    [Tooltip("Shuffle the order of non-boss waves within each sector. The boss " +
             "wave always stays last regardless of this setting.")]
    [SerializeField] private bool randomizeWaveOrder = true;

    [Header("Scaling")]
    [SerializeField] private List<StatModifierEffect> enemyScalerEffects = new List<StatModifierEffect>();

    private BoxCollider2D spawnCollider;

    private List<SectorDefinition> sectorQueue = new List<SectorDefinition>();
    private int sectorQueueIndex;
    private bool endlessMode;

    private SectorDefinition currentSector;
    private List<WaveDefinition> currentWaveOrder = new List<WaveDefinition>();
    private int currentWaveIndex;
    private WaveDefinition currentWave;

    private List<int> remainingCounts = new List<int>();
    private int spawnedThisWave;
    private float spawnTimer;
    private readonly List<Enemy> aliveEnemies = new List<Enemy>();

    private float xpCounter;

    private int waveCounter;
    public int WaveCounter => waveCounter;
    public static EnemySpawnController Instance => instance;

    public event Action<WaveDefinition, int> OnWaveChanged;

    public String CurrentSectorName => currentSector.SectorName;

    private void Awake()
    {
        spawnCollider = GetComponent<BoxCollider2D>();
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        BuildSectorQueue(randomizeSectorOrder);
        AdvanceToNextSector();
    }

    private void Update()
    {
        if (currentWave == null) return;

        bool doneSpawning = spawnedThisWave >= currentWave.TotalEnemyCount;

        if (!doneSpawning)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentWave.SpawnCooldown && aliveEnemies.Count < currentWave.MaxConcurrentEnemies)
            {
                TrySpawnFromWave();
                spawnTimer = 0f;
            }
        }
        else if (aliveEnemies.Count == 0)
        {
            OnWaveCleared();
        }
    }

    // ---------------------------------------------------------------
    // Sector / wave progression
    // ---------------------------------------------------------------

    private void BuildSectorQueue(bool shuffle)
    {
        sectorQueue = new List<SectorDefinition>(sectors);
        if (shuffle) Shuffle(sectorQueue);
        sectorQueueIndex = 0;
    }

    private void AdvanceToNextSector()
    {
        if (sectorQueueIndex >= sectorQueue.Count)
        {
            // Ran out of non-repeating sectors -> endless mode, sectors can repeat.
            endlessMode = true;
            BuildSectorQueue(randomizeEndlessOrder);
        }

        if (sectorQueue.Count == 0)
        {
            Debug.LogWarning("[EnemySpawnController] No sectors assigned.");
            return;
        }

        currentSector = sectorQueue[sectorQueueIndex];
        sectorQueueIndex++;
        currentWaveOrder = BuildWaveOrder(currentSector, randomizeWaveOrder);
        currentWaveIndex = 0;
        StartWave(currentWaveOrder[currentWaveIndex]);
    }

    // Builds the play order for a sector's waves. The boss wave (assumed to be
    // the last entry in SectorDefinition, per its own validation) is always kept last;
    // everything before it is optionally shuffled so waves don't play in the
    // same order every time the sector is visited.
    private List<WaveDefinition> BuildWaveOrder(SectorDefinition sector, bool shuffle)
    {
        var waves = new List<WaveDefinition>(sector.Waves);
        if (waves.Count == 0) return waves;

        WaveDefinition boss = waves[waves.Count - 1];
        waves.RemoveAt(waves.Count - 1);

        if (shuffle) Shuffle(waves);

        waves.Add(boss);
        return waves;
    }

    private void StartWave(WaveDefinition wave)
    {
        currentWave = wave;
        spawnedThisWave = 0;
        spawnTimer = wave.SpawnCooldown; // spawn first enemy immediately
        aliveEnemies.Clear();

        remainingCounts.Clear();
        foreach (var entry in wave.EnemyEntries)
            remainingCounts.Add(Mathf.Max(0, entry.count));

        waveCounter++;
        OnWaveChanged?.Invoke(currentWave, waveCounter);

        // Edge case: a wave with no enemies configured clears itself instantly.
        if (wave.TotalEnemyCount == 0)
            OnWaveCleared();
    }

    private void OnWaveCleared()
    {
        xpCounter += currentWave.TotalXP;

        currentWaveIndex++;
        if (currentWaveIndex < currentWaveOrder.Count)
        {
            StartWave(currentWaveOrder[currentWaveIndex]);
        }
        else
        {
            currentWave = null;
            AdvanceToNextSector();
        }
    }

    // ---------------------------------------------------------------
    // Spawning
    // ---------------------------------------------------------------

    private void TrySpawnFromWave()
    {
        int entryIndex = PickWeightedRemainingEntry();
        if (entryIndex < 0) return; // nothing left to spawn (shouldn't happen if doneSpawning check is correct)

        Enemy prefab = currentWave.EnemyEntries[entryIndex].enemyPrefab;
        if (prefab == null) return;

        remainingCounts[entryIndex]--;
        spawnedThisWave++;
        SpawnEnemy(prefab);
    }

    // Picks a random entry among those that still have enemies left to spawn.
    private int PickWeightedRemainingEntry()
    {
        int totalRemaining = 0;
        foreach (var r in remainingCounts) totalRemaining += r;
        if (totalRemaining <= 0) return -1;

        int roll = UnityEngine.Random.Range(0, totalRemaining);
        for (int i = 0; i < remainingCounts.Count; i++)
        {
            if (roll < remainingCounts[i]) return i;
            roll -= remainingCounts[i];
        }
        return -1;
    }

    private void SpawnEnemy(Enemy prefab)
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        Enemy enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        float scalingFraction = Mathf.Pow(ENEMY_SCALING_COEFFICIENT, waveCounter) - 1f;

        foreach (var effect in enemyScalerEffects)
            enemy.GetComponent<EffectManager>().AddEffect(effect.Stacked(scalingFraction));

        enemy.OnDeath += () => RemoveEnemy(enemy);
        aliveEnemies.Add(enemy);
    }

    private void RemoveEnemy(Enemy enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnCollider.bounds;
        float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}