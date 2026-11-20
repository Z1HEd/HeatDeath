using UnityEngine;
using System.Collections.Generic;

public class MissileModule : ProjectileModule
{
    [Tooltip("The diameter of each missile's blast.")]
    [SerializeField] private ScalarStat blastDiameter = new ScalarStat(StatType.BlastDiameter, 1f, 0f);
    [Tooltip("How far a missile looks for a target to steer toward. 0: it flies straight.")]
    [SerializeField] private ScalarStat seekingRange = new ScalarStat(StatType.SeekingRange, 1f, 0f);
    [Tooltip("The launcher with a rocket ready and without one. Leave both empty to keep its sprite.")]
    [SerializeField] private Sprite loadedSprite;
    [SerializeField] private Sprite emptySprite;
    private SpriteRenderer spriteRenderer;

    public float BlastDiameter => blastDiameter;
    public float SeekingRange => seekingRange;
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // The rocket shows once the launcher is half reloaded. Without the sprites (Vacuum Cannon), its sprite stays.
    protected override void Update()
    {
        base.Update();
        if (loadedSprite != null)
            spriteRenderer.sprite = fireCooldown < fireCooldown.MaxValue / 2 ? loadedSprite : emptySprite;
    }

    protected override void CollectStats(List<IStat> stats)
    {
        base.CollectStats(stats);

        stats.Add(blastDiameter);
        stats.Add(seekingRange);
    }
}