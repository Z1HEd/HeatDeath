using UnityEngine;

[CreateAssetMenu(fileName = "StatDefinition", menuName = "HeatDeath/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string key;

    public string DisplayName => displayName;
    public string Key => key;
}
