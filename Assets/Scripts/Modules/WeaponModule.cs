using UnityEngine;

[RequireComponent(typeof(Ship))]
public abstract class WeaponModule : ModuleBase
{
    [SerializeField] protected float fireRate = 1f;
    protected float lastFireTime;

    protected int DetectLayer { get; private set; }

    protected override void Start()
    {
        base.Start();
        DetermineWeaponLayer();
    }

    protected virtual float FireDelay => 1f / Mathf.Max(fireRate, 0.0001f);
    protected virtual bool CanFire => Time.time >= lastFireTime + FireDelay;

    protected virtual void Update() {}

    protected virtual void Fire() { lastFireTime = Time.time; }

    private void DetermineWeaponLayer()
    {
        DetectLayer = GetLayerForShip("DetectPlayer", "DetectEnemy");
    }

    private int GetLayerForShip(string playerLayerName, string enemyLayerName)
    {
        if (ship is Player)
            return LayerMask.NameToLayer(enemyLayerName);

        if (ship is Enemy)
            return LayerMask.NameToLayer(playerLayerName);

        return 0;
    }
}

