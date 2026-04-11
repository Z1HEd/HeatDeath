using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ModuleDefinition", menuName = "HeatDeath/Module Definition")]
public class ModuleDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string key;
    [SerializeField] private string componentTypeName;

    public string DisplayName => displayName;
    public string Key => key;
    public string ComponentTypeName => componentTypeName;

    public Type ResolveComponentType()
    {
        Type direct = Type.GetType(componentTypeName);
        if (direct != null)
            return direct;

        return Type.GetType(componentTypeName + ", Assembly-CSharp");
    }
}
