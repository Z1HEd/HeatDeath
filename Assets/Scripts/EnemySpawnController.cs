using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawnController : MonoBehaviour
{
    private static EnemySpawnController instance;

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
    [Tooltip("Enemy multiplier at the start of sector 1, 2, 3... It grows geometrically between keys. " +
             "Run progress counts sectors played, never which sector it is.")]
    [SerializeField] private List<float> enemyMultiplierAtSectorStart = new List<float> { 1f, 3.5f, 9f, 20f, 30f, 42f };
    [Tooltip("Enemy multiplier growth per sector past the last key.")]
    [SerializeField] private float endlessMultiplierGrowth = 1.3f;
    [Tooltip("Enemy scrap rewards grow by this fraction of their base value per sector of run progress.")]
    [SerializeField] private float xpRewardGrowthPerSector = 0.3f;
    [Tooltip("Each spawned enemy gets these stacked by (enemy multiplier - 1), so +100% gives exactly the multiplier.")]
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
    private readonly List<Enemy> waveBosses = new List<Enemy>();
    // Lowest point the bosses reached, so shield regen can't take back released escorts.
    private float lowestBossFraction = 1f;

    // Scaling state, set once per wave in StartWave.
    private int sectorsStarted;
    private float waveEnemyMultiplier = 1f;
    private float waveXPMultiplier = 1f;

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
 
            // The escort check runs every frame, so it sees the bosses' lowest point.
            if (!IsEscortHeld() && spawnTimer >= dynamicCooldown && aliveEnemies.Count < currentWave.MaxConcurrentEnemies)
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
        sectorsStarted++;
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
        waveBosses.Clear();
        lowestBossFraction = 1f;

        float progress = sectorsStarted - 1 + (float)currentWaveIndex / currentWaveOrder.Count;
        waveEnemyMultiplier = EnemyMultiplier(progress, enemyMultiplierAtSectorStart, endlessMultiplierGrowth);
        waveXPMultiplier = XPRewardMultiplier(progress, xpRewardGrowthPerSector);

        remainingCounts.Clear();
        foreach (var entry in wave.EnemyEntries)
            remainingCounts.Add(Mathf.Max(0, entry.count));

        waveCounter++;
        OnWaveChanged?.Invoke(currentWave, waveCounter);

        if (wave.TotalEnemyCount == 0)
            OnWaveCleared();
    }

    // M(p): geometric between the sector-start keys, then ×endlessGrowth per sector past the last key.
    public static float EnemyMultiplier(float progress, IReadOnlyList<float> keys, float endlessGrowth)
    {
        if (keys == null || keys.Count == 0)
            return 1f;

        int last = keys.Count - 1;
        if (progress >= last)
            return keys[last] * Mathf.Pow(endlessGrowth, progress - last);

        progress = Mathf.Max(0f, progress);
        int i = Mathf.FloorToInt(progress);
        return keys[i] * Mathf.Pow(keys[i + 1] / keys[i], progress - i);
    }

    public static float XPRewardMultiplier(float progress, float growthPerSector)
    {
        return 1f + growthPerSector * progress;
    }

    private void OnWaveCleared()
    {
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

    // Dev shortcut: removes the wave's enemies without rewards and starts the next wave at once.
    // During the pause between waves, it starts the upcoming wave at once instead.
    public void SkipWave()
    {
        if (currentWaveOrder.Count == 0)
            return;

        StopAllCoroutines();
        bool waveActive = currentWave != null;
        ClearEnemiesWithoutRewards();
        currentWave = null;

        if (waveActive)
            currentWaveIndex++;

        if (currentWaveIndex < currentWaveOrder.Count)
            StartWave(currentWaveOrder[currentWaveIndex]);
        else
            AdvanceToNextSector();
    }

    // Dev shortcut: starts sector N (from 1) as if N - 1 sectors had been played, so scaling uses its progress.
    public void JumpToSector(int sector)
    {
        if (sector < 1 || sectorQueue.Count == 0)
            return;

        StopAllCoroutines();
        ClearEnemiesWithoutRewards();
        currentWave = null;

        sectorsStarted = sector - 1;
        endlessMode = sector > sectors.Count;
        // Past the list, AdvanceToNextSector rebuilds the queue for endless mode.
        sectorQueueIndex = endlessMode ? sectorQueue.Count : sector - 1;
        AdvanceToNextSector();
    }

    // Destroy rather than Die, so no scrap is awarded.
    private void ClearEnemiesWithoutRewards()
    {
        foreach (var enemy in aliveEnemies)
            if (enemy != null)
                Destroy(enemy.gameObject);

        aliveEnemies.Clear();
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
        Enemy enemy = SpawnEnemy(prefab);
        if (currentWave.EnemyEntries[entryIndex].isBoss)
            waveBosses.Add(enemy);
    }

    // Boss waves: bosses spawn first, then bossEscortAtStart others. The rest are released in
    // bossEscortReleaseSteps even groups as the bosses lose health + shields, all of them by
    // releaseAllAtBossFraction, so a boss never fights alone.
    // Dead bosses count as 0, so every escort is released by the time they die and the wave's scrap stays fixed.
    private bool IsEscortHeld()
    {
        int bossCount = currentWave.BossCount;
        if (bossCount == 0 || waveBosses.Count < bossCount)
            return false;

        float fraction = 0f;
        foreach (var boss in waveBosses)
            if (boss != null && !boss.IsDead && boss.CoreModule != null)
                fraction += boss.CoreModule.DurabilityFraction;
        lowestBossFraction = Mathf.Min(lowestBossFraction, fraction / bossCount);

        int escorts = currentWave.TotalEnemyCount - bossCount;
        int atStart = Mathf.Min(currentWave.BossEscortAtStart, escorts);
        int steps = currentWave.BossEscortReleaseSteps;
        float progress = Mathf.InverseLerp(1f, currentWave.ReleaseAllAtBossFraction, lowestBossFraction);
        float releasedShare = Mathf.Floor(progress * steps) / steps;
        int released = atStart + Mathf.FloorToInt((escorts - atStart) * releasedShare);

        return spawnedThisWave - bossCount >= released;
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

    private Enemy SpawnEnemy(Enemy prefab)
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        Enemy enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        float scalingFraction = waveEnemyMultiplier - 1f;
        foreach (var effect in enemyScalerEffects)
            enemy.GetComponent<EffectManager>().AddEffect(effect.Stacked(scalingFraction));

        enemy.SetXPMultiplier(waveXPMultiplier);

        enemy.OnDeath += () => RemoveEnemy(enemy);
        aliveEnemies.Add(enemy);
        return enemy;
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