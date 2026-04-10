using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class ModuleManager : MonoBehaviour
{
    protected Ship ship;
    private List<ModuleBase> modules = new List<ModuleBase>();

    public void Start()
    {
        ship = GetComponent<Ship>();
    }

    public void AddModule(ModuleBase module)
    {
        if (module != null && !modules.Contains(module))
            modules.Add(module);
    }

    public void RemoveModule(ModuleBase module)
    {
        if (module != null && modules.Contains(module))
            modules.Remove(module);
    }

    public List<T> GetModules<T>() where T : ModuleBase
    {
        var result = new List<T>();
        foreach (var module in modules)
        {
            if (module is T typedModule)
                result.Add(typedModule);
        }
        return result;
    }

    public T GetFirstModule<T>() where T : ModuleBase
    {
        foreach (var module in modules)
        {
            if (module is T typedModule)
                return typedModule;
        }
        return null;
    }

    public HashSet<ModuleDefinition> GetInstalledModuleDefinitions()
    {
        var result = new HashSet<ModuleDefinition>();
        foreach (var module in modules)
        {
            if (module == null || module.ModuleDefinition == null)
                continue;

            result.Add(module.ModuleDefinition);
        }

        return result;
    }

    // Backward compatibility: expose movement modules through generic accessor
    public List<MovementModule> MovementModules => GetModules<MovementModule>();
}
