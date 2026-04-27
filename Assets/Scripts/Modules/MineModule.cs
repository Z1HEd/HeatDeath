using System.Collections.Generic;
using UnityEngine;

public class MineModule : ProjectileModule
{
    private readonly List<MineProjectile> activeMines = new List<MineProjectile>();

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
}
