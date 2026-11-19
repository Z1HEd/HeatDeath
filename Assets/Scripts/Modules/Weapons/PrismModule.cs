using UnityEngine;

// Prism: the Laser split into a fixed fan. Each beam takes its share of the spectrum, so the fan runs from red on the
// left toward violet on the right. The colours show with a rainbow beam material (LaserBeam shader, _Rainbow).
public class PrismModule : ProjectileModule
{
    private static readonly int HueStartId = Shader.PropertyToID("_HueStart");
    private static readonly int HueEndId = Shader.PropertyToID("_HueEnd");

    [Header("Prism")]
    [Tooltip("Hues the fan spans together, from red (0) toward violet.")]
    [SerializeField] private float spectrum = 0.8f;

    protected override void OnProjectileSpawned(Projectile projectile, int direction, int directions)
    {
        var beam = projectile.GetComponentInChildren<SpriteRenderer>();
        if (beam == null)
            return;

        int fromLeft = directions - 1 - direction;
        var block = new MaterialPropertyBlock();
        beam.GetPropertyBlock(block);
        block.SetFloat(HueStartId, spectrum * fromLeft / directions);
        block.SetFloat(HueEndId, spectrum * (fromLeft + 1) / directions);
        beam.SetPropertyBlock(block);
    }
}
