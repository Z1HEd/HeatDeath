using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ship))]
public class ModuleManager : MonoBehaviour
{
    protected Ship ship;
    private List<ModuleBase> modules = new List<ModuleBase>();
    private List<WeaponDefinition> weaponDefinitions = new List<WeaponDefinition>();

    public List<MovementModule> MovementModules => GetModules<MovementModule>();
    public List<WeaponModule> WeaponModules => GetModules<WeaponModule>();
    [SerializeField]
    protected Transform turretMounts;
    public event Action<WeaponDefinition> OnWeaponAdded;
    // The old module and its replacement, raised before the old one is destroyed.
    public event Action<ModuleBase, ModuleBase> OnModuleReplaced;

    public void Start()
    {
        ship = GetComponent<Ship>();
        if (turretMounts == null)
            turretMounts = transform.Find("TurretMounts");
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

    public bool HasModule(ModuleDefinition definition)
    {
        if (definition == null) return false;
        foreach (var module in modules)
        {
            if (module != null && module.ModuleDefinition == definition)
                return true;
        }
        return false;
    }

    // The installed definition that is this one or builds on it (a ship's core on the shared core), or null.
    public ModuleDefinition FindInstalled(ModuleDefinition definition)
    {
        if (definition == null) return null;
        foreach (var module in modules)
        {
            ModuleDefinition installed = module != null ? module.ModuleDefinition : null;
            if (installed != null && (installed == definition || installed.BaseDefinition == definition))
                return installed;
        }
        return null;
    }

    public WeaponModule AddWeapon(WeaponDefinition definition)
    {
        foreach (Transform mount in turretMounts){
            if (mount.childCount>0) continue;
            var weapon = Instantiate(definition.WeaponPrefab, mount);
            if (!weaponDefinitions.Contains(definition)){
                GameUIController.Instance.BindWeaponButton(weapon,weaponDefinitions.Count);
                weaponDefinitions.Add(definition);
            }
            OnWeaponAdded?.Invoke(definition);
            return weapon.GetComponent<WeaponModule>();
        }
        Debug.LogError("Failed to add weapon: missing turret mount");
        return null;
    }

    // Swaps every installed module with this definition (a weapon and Yamato's copy of it) for an instance of
    // the prefab in the same parent. The prefab keeps the definition, so the module's upgrades apply to it too.
    public void ReplaceModule(ModuleDefinition definition, GameObject prefab)
    {
        foreach (var old in modules.FindAll(m => m != null && m.ModuleDefinition == definition))
        {
            var replacement = Instantiate(prefab, old.transform.parent).GetComponent<ModuleBase>();
            if (replacement == null)
            {
                Debug.LogError($"Replacement prefab {prefab.name} has no module");
                return;
            }

            // Now rather than in Start, like AddShipModule.
            AddModule(replacement);
            RemoveModule(old);
            OnModuleReplaced?.Invoke(old, replacement);
            Destroy(old.gameObject);
        }
    }

    // Ship modules sit on the ship root, with no mount and no HUD button.
    public ShipModule AddShipModule(ShipModuleDefinition definition)
    {
        var module = Instantiate(definition.ModulePrefab, transform).GetComponent<ShipModule>();
        if (module == null)
            Debug.LogError($"Ship module prefab for {definition.DisplayName} has no ShipModule");

        // Now rather than in its Start, so a second draft in the same frame doesn't offer it again.
        AddModule(module);
        return module;
    }
}
