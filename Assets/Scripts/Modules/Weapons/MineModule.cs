using System.Collections.Generic;
using UnityEngine;

public class MineModule : ProjectileModule
{
    [SerializeField] public ScalarStat ExplosionRange = new ScalarStat(StatType.ExplosionRange,1f,0f);
    [SerializeField]private readonly List<MineProjectile> activeMines = new List<MineProjectile>();

    protected override void OnProjectileSpawned(Projectile projectile)
    {
        
        activeMines.RemoveAll(m => m == null);
        activeMines.Add(projectile as MineProjectile);

        while (activeMines.Count > Mathf.FloorToInt(projectileCount))
        {
            MineProjectile oldest = activeMines[0];
            activeMines.RemoveAt(0);
            if (oldest != null)
                oldest.ForceExplode();
        }
    }
    // projectileCount is treated as max num of mines on map.
    protected override void SpawnProjectiles(Vector3 spawnPosition, Vector3 aimDirection)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab not assigned on " + gameObject.name);
            return;
        }

        int count = Mathf.Max(1, Mathf.FloorToInt(projectileCount));
        float spreadDegrees = Mathf.Max(0f, projectileSpread);
        
        float angleOffset = GetSpreadAngleOffset(spreadDegrees);
        Vector3 shotDirection = Quaternion.AngleAxis(angleOffset, Vector3.forward) * aimDirection;

        Projectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        projectile.gameObject.layer = DetectLayer;
        projectile.Initialize(shotDirection * projectileSpeed, this);
        OnProjectileSpawned(projectile);
    }
    protected override void ApplyModifiers(IReadOnlyDictionary<StatType, StatModifier> modifiers)
    {
        base.ApplyModifiers(modifiers);
        ExplosionRange.Recalculate(modifiers);
    }

    protected override void ResetModifiers()
    {
        base.ResetModifiers();
        ExplosionRange.ResetToBase();
    }
}
