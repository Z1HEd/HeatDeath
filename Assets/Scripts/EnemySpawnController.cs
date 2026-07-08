using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawnController : MonoBehaviour
{
    private static EnemySpawnController instance;
    public const float ENEMY_SCALING_COEFFICIENT = 1.25f;
    public const float XP_PER_LEVEL = 100f;

    [Header("Pacing")]
    [Tooltip("Pause before each wave (after the first wave of a sector).")]
    [SerializeField] private float waveEntryDelay = 2f;
    [Tooltip("Pause before the first wave of a newly entered sector.")]
    [SerializeField] private float sectorEntryDelay = 3f;

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

    // Sector/wave progression state
    private List<SectorDefinition> sectorQueue = new List<SectorDefinition>();
    private int sectorQueueIndex;
    private bool endlessMode;

    private SectorDefinition currentSector;
    private List<WaveDefinition> currentWaveOrder = new List<WaveDefinition>();
    private int currentWaveIndex;
    private WaveDefinition currentWave;

    // Wave spawning state
    private List<int> remainingCounts = new List<int>();
    private int spawnedThisWave;
    private float spawnTimer;
    private readonly List<Enemy> aliveEnemies = new List<Enemy>();

    private float xpCounter;
    private int waveCounter;
    public int WaveCounter => waveCounter;

    public event Action<WaveDefinition, int> OnWaveChanged;
    public String CurrentSectorName => currentSector.SectorName;
    public static EnemySpawnController Instance => instance;

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
 
            // SpawnCooldown is the cooldown at (maxConcurrentEnemies - 1) alive;
            // it scales down linearly to 0 (instant) at 0 alive.
            int denom = currentWave.MaxConcurrentEnemies - 1;
            float dynamicCooldown = denom > 0
                ? currentWave.SpawnCooldown * aliveEnemies.Count / denom
                : 0f;
 
            if (spawnTimer >= dynamicCooldown && aliveEnemies.Count < currentWave.MaxConcurrentEnemies)
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
        currentWave = null;
        StartCoroutine(StartWaveAfterDelay(currentWaveOrder[currentWaveIndex], sectorEntryDelay));
    }

    private List<WaveDefinition> BuildWaveOrder(SectorDefinition sector, bool shuffle)
    {
        var groups = new SortedDictionary<int, List<WaveDefinition>>();
        foreach (var wave in sector.Waves)
        {
            if (wave == null) continue;
            if (!groups.TryGetValue(wave.WaveOrder, out var group))
            {
                group = new List<WaveDefinition>();
                groups[wave.WaveOrder] = group;
            }
            group.Add(wave);
        }

        var result = new List<WaveDefinition>();
        foreach (var kvp in groups)
        {
            var group = kvp.Value;
            if (shuffle) Shuffle(group);
            result.AddRange(group);
        }
        return result;
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

        if (wave.TotalEnemyCount == 0)
            OnWaveCleared();
    }

    private void OnWaveCleared()
    {
        xpCounter += currentWave.TotalXP;

        currentWaveIndex++;
        if (currentWaveIndex < currentWaveOrder.Count)
        {
            currentWave = null;
            StartCoroutine(StartWaveAfterDelay(currentWaveOrder[currentWaveIndex], waveEntryDelay));
        }
        else
        {
            currentWave = null;
            AdvanceToNextSector();
        }
    }

    // currentWave stays null for the duration of the pause, so Update() simply
    // idles (no spawning, no clear-checks) until the delay elapses.
    private IEnumerator StartWaveAfterDelay(WaveDefinition wave, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        StartWave(wave);
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

   private int PickWeightedRemainingEntry()
    {
        var entries = currentWave.EnemyEntries;
 
        int bossTotal = 0;
        for (int i = 0; i < remainingCounts.Count; i++)
        {
            if (entries[i].isBoss) bossTotal += remainingCounts[i];
        }
 
        if (bossTotal > 0)
        {
            int bossRoll = UnityEngine.Random.Range(0, bossTotal);
            for (int i = 0; i < remainingCounts.Count; i++)
            {
                if (!entries[i].isBoss) continue;
                if (bossRoll < remainingCounts[i]) return i;
                bossRoll -= remainingCounts[i];
            }
        }
 
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

        float levelEquivalent = xpCounter / XP_PER_LEVEL;
        float scalingFraction = Mathf.Pow(ENEMY_SCALING_COEFFICIENT, levelEquivalent) - 1f;

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