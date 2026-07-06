using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a sector as an ordered sequence of waves. The last wave should be
/// a boss wave; this is validated (not enforced) in the editor.
/// </summary>
[CreateAssetMenu(menuName = "HeatDeath/Sector Definition")]
public class SectorDefinition : ScriptableObject
{
    [SerializeField] private string sectorName;

    [Tooltip("Waves play in this order. The last wave should have IsBossWave = true.")]
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    public string SectorName => sectorName;
    public IReadOnlyList<WaveDefinition> Waves => waves;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (waves.Count == 0) return;

        for (int i = 0; i < waves.Count - 1; i++)
        {
            if (waves[i] == null)
                Debug.LogWarning($"[SectorData] '{name}' has an unassigned wave", this);
        }
    }
#endif
}
