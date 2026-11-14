using UnityEngine;

// One way a module can evolve, listed on the module's definition. Its pool is the evolution upgrades that point at it.
[CreateAssetMenu(fileName = "EvolutionPath", menuName = "HeatDeath/Evolution Path")]
public class EvolutionPathDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [Tooltip("The evolved version that replaces the module: a weapon or ship-module prefab, or for a ship's core a " +
             "ship prefab (not supported yet). It keeps the module's definition, so slotted upgrades still apply.")]
    [SerializeField] private GameObject evolvedPrefab;

    public string DisplayName => displayName;
    public string Description => description;
    public GameObject EvolvedPrefab => evolvedPrefab;
}
