using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "HeatDeath/Sector Definition")]
public class SectorDefinition : ScriptableObject
{
    // The budget every sector must meet: Docs/Balance.md, "Content budgets".
    public const float StandardBaseXP = 600f;
    public const float BaseXPTolerance = 0.02f;
    public const int MinWaves = 8;
    public const int MaxWaves = 10;

    [SerializeField] private string sectorName;

    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    [ReadOnly, SerializeField] private float cachedTotalXP;
    [ReadOnly, SerializeField] private int cachedTotalEnemyCount;

    public string SectorName => sectorName;
    public IReadOnlyList<WaveDefinition> Waves => waves;

#if UNITY_EDITOR
    // Doesn't rerun when a wave asset changes; use the context menu then.
    private void OnValidate()
    {
        RecalculateAndCheck();
    }

    [ContextMenu("Recalculate totals")]
    private void RecalculateFromMenu()
    {
        RecalculateAndCheck();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void RecalculateAndCheck()
    {
        cachedTotalXP = 0f;
        cachedTotalEnemyCount = 0;
        var assigned = new List<WaveDefinition>();
        int lastOrder = int.MinValue;

        foreach (var wave in waves)
        {
            if (wave == null)
            {
                Debug.LogWarning($"[SectorData] '{name}' has an unassigned wave", this);
                continue;
            }

            assigned.Add(wave);
            cachedTotalXP += wave.TotalXP;
            cachedTotalEnemyCount += wave.TotalEnemyCount;
            lastOrder = Mathf.Max(lastOrder, wave.WaveOrder);
        }

        if (assigned.Count == 0)
            return;

        if (assigned.Count < MinWaves || assigned.Count > MaxWaves)
            Debug.LogWarning($"[SectorData] '{name}' has {assigned.Count} waves; it needs {MinWaves}-{MaxWaves}", this);

        if (Mathf.Abs(cachedTotalXP - StandardBaseXP) > StandardBaseXP * BaseXPTolerance)
            Debug.LogWarning($"[SectorData] '{name}' gives {cachedTotalXP} base scrap; it needs {StandardBaseXP}", this);

        List<WaveDefinition> lastGroup = assigned.FindAll(w => w.WaveOrder == lastOrder);
        if (lastGroup.Count != 1)
            Debug.LogWarning($"[SectorData] '{name}' has {lastGroup.Count} waves with the highest waveOrder; " +
                             "the boss wave must be alone in it", this);
        else if (!lastGroup[0].HasBoss)
            Debug.LogWarning($"[SectorData] '{name}': the last wave '{lastGroup[0].name}' has no boss entry", this);
    }
#endif
}
