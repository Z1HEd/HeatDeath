using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "HeatDeath/Sector Definition")]
public class SectorDefinition : ScriptableObject
{
    [SerializeField] private string sectorName;

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
