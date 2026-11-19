using System.Collections.Generic;
using UnityEngine;

// On a StatusVisual prefab: while it's on a ship, its thrusters pour out plain grey smoke instead of fire: clouds
// that billow out, darken, and thin away over their longer life. Copies ShipAppearance makes for thruster colours
// ("(Clone)") take their size and life from their original, so nothing is changed twice, and everything returns to
// normal when it's removed.
public class ThrusterSmoke : MonoBehaviour
{
    [Tooltip("The smoke's colour as it leaves the engine; alpha is how see-through it starts.")]
    [SerializeField] private Color color = new Color(0.5f, 0.5f, 0.52f, 0.75f);
    [Tooltip("The colour it darkens to by the end of its life, while it fades out.")]
    [SerializeField] private Color endColor = new Color(0.18f, 0.18f, 0.2f);
    [Tooltip("Start size relative to the fire's.")]
    [SerializeField] private float sizeMultiplier = 1.2f;
    [Tooltip("How many times its start size each cloud grows to by the end of its life.")]
    [SerializeField] private float growth = 2.5f;
    [SerializeField] private float lifetimeMultiplier = 2f;

    private struct Original
    {
        public ParticleSystem.MinMaxGradient StartColor;
        public ParticleSystem.MinMaxGradient ColorOverLifetime;
        public bool ColorOverLifetimeEnabled;
        public ParticleSystem.MinMaxCurve SizeOverLifetime;
        public bool SizeOverLifetimeEnabled;
        public bool SizeSeparateAxes;
        public float Size;
        public float Lifetime;
    }

    private Ship ship;
    private readonly Dictionary<ParticleSystem, Original> originals = new Dictionary<ParticleSystem, Original>();

    private void Start() => ship = GetComponentInParent<Ship>();

    private void Update()
    {
        if (ship == null)
            return;
        foreach (var movement in ship.moduleManager.MovementModules)
        foreach (var thruster in movement.GetComponentsInChildren<ParticleSystem>())
            if (!originals.ContainsKey(thruster))
                Apply(thruster);
    }

    private void Apply(ParticleSystem thruster)
    {
        var main = thruster.main;
        var overLifetime = thruster.colorOverLifetime;
        var sizeOverLifetime = thruster.sizeOverLifetime;
        var original = new Original
        {
            StartColor = main.startColor,
            ColorOverLifetime = overLifetime.color,
            ColorOverLifetimeEnabled = overLifetime.enabled,
            SizeOverLifetime = sizeOverLifetime.size,
            SizeOverLifetimeEnabled = sizeOverLifetime.enabled,
            SizeSeparateAxes = sizeOverLifetime.separateAxes,
            Size = main.startSizeMultiplier,
            Lifetime = main.startLifetimeMultiplier,
        };
        // A clone was copied from an original already turned to smoke: take its size and life from the original.
        // Its colour over lifetime is its own (ShipAppearance just set it).
        if (CloneSource(thruster, out var source))
        {
            var sourceOriginal = originals[source];
            original.StartColor = sourceOriginal.StartColor;
            original.SizeOverLifetime = sourceOriginal.SizeOverLifetime;
            original.SizeOverLifetimeEnabled = sourceOriginal.SizeOverLifetimeEnabled;
            original.SizeSeparateAxes = sourceOriginal.SizeSeparateAxes;
            original.Size = sourceOriginal.Size;
            original.Lifetime = sourceOriginal.Lifetime;
        }
        originals[thruster] = original;

        main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
        main.startSizeMultiplier = original.Size * sizeMultiplier;
        main.startLifetimeMultiplier = original.Lifetime * lifetimeMultiplier;
        overLifetime.enabled = true;
        overLifetime.color = new ParticleSystem.MinMaxGradient(Darkening());
        // Grows from its start size to growth times it; the curve stays within 0-1 and the multiplier scales it.
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.separateAxes = false;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(growth, AnimationCurve.Linear(0f, 1f / growth, 1f, 1f));
    }

    // From the smoke's colour to the darker end colour, thinning out to nothing by the end of each cloud's life.
    private Gradient Darkening()
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(endColor, 1f) },
                         new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a * 0.5f, 0.5f),
                                 new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    private bool CloneSource(ParticleSystem thruster, out ParticleSystem source)
    {
        source = null;
        if (!thruster.name.EndsWith("(Clone)"))
            return false;
        string originalName = thruster.name.Substring(0, thruster.name.Length - "(Clone)".Length);
        foreach (var known in originals.Keys)
            if (known != null && known.name == originalName && known.transform.parent == thruster.transform.parent)
            {
                source = known;
                return true;
            }
        return false;
    }

    private void OnDestroy()
    {
        foreach (var pair in originals)
        {
            if (pair.Key == null)
                continue;
            var main = pair.Key.main;
            main.startColor = pair.Value.StartColor;
            main.startSizeMultiplier = pair.Value.Size;
            main.startLifetimeMultiplier = pair.Value.Lifetime;
            var overLifetime = pair.Key.colorOverLifetime;
            overLifetime.color = pair.Value.ColorOverLifetime;
            overLifetime.enabled = pair.Value.ColorOverLifetimeEnabled;
            var sizeOverLifetime = pair.Key.sizeOverLifetime;
            sizeOverLifetime.size = pair.Value.SizeOverLifetime;
            sizeOverLifetime.separateAxes = pair.Value.SizeSeparateAxes;
            sizeOverLifetime.enabled = pair.Value.SizeOverLifetimeEnabled;
        }
    }
}
