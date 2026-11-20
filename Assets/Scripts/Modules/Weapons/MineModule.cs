using System.Collections.Generic;
using UnityEngine;

public class MineModule : ProjectileModule
{
    [Tooltip("The diameter of each mine's blast.")]
    [SerializeField] private ScalarStat blastDiameter = new ScalarStat(StatType.BlastDiameter, 1f, 0f);
    private readonly List<MineProjectile> activeMines = new List<MineProjectile>();

    public float BlastDiameter => blastDiameter;

    // One mine per throw: Projectile Count is the most mines on the field.
    protected override void SpawnProjectiles(Vector3 position, Vector3 aimDirection) =>
        SpawnProjectile(position, aimDirection, 0, 1);

    // A mine past the most allowed sets off the oldest.
    protected override void OnProjectileSpawned(Projectile projectile, int direction, int directions)
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
    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(blastDiameter);
    }
}
