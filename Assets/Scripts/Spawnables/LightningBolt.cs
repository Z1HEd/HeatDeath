using UnityEngine;

// One hop of a Tesla chain, drawn by the Custom/Lightning shader. Follows both endpoints,
// re-rolls the bolt shape every jitterInterval, fades out over its duration, then destroys itself.
[RequireComponent(typeof(LineRenderer))]
public class LightningBolt : MonoBehaviour
{
    private static readonly int StartId = Shader.PropertyToID("_Start");
    private static readonly int EndId = Shader.PropertyToID("_End");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int AmplitudeId = Shader.PropertyToID("_Amplitude");
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");

    private LineRenderer line;
    private MaterialPropertyBlock block;
    private Transform from;
    private Transform to;
    private Vector3 fromPosition;
    private Vector3 toPosition;
    private float duration;
    private float jitterInterval;
    private float elapsed;
    private float jitterTimer;
    private float seed;
    private float padding;

    // A null or destroyed endpoint transform keeps its last known position.
    public static LightningBolt Spawn(Transform from, Vector3 fromPosition, Transform to, float duration,
        float jitterInterval, Material material, int sortingLayerID, int sortingOrder)
    {
        var bolt = new GameObject("Lightning").AddComponent<LightningBolt>();
        bolt.Setup(from, fromPosition, to, duration, jitterInterval, material, sortingLayerID, sortingOrder);
        return bolt;
    }

    private void Setup(Transform from, Vector3 fromPosition, Transform to, float duration,
        float jitterInterval, Material material, int sortingLayerID, int sortingOrder)
    {
        this.from = from;
        this.to = to;
        this.fromPosition = fromPosition;
        toPosition = to != null ? to.position : fromPosition;
        this.duration = Mathf.Max(duration, 0.01f);
        this.jitterInterval = jitterInterval;

        line = GetComponent<LineRenderer>();
        block = new MaterialPropertyBlock();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.sortingLayerID = sortingLayerID;
        line.sortingOrder = sortingOrder;

        // The quad is only a canvas for the shader: wide enough for the zigzag plus its glow,
        // and extended past both ends so the glow caps aren't cut off.
        padding = material.GetFloat(GlowWidthId);
        line.startWidth = line.endWidth = 2f * (material.GetFloat(AmplitudeId) + padding);

        Tick(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        jitterTimer -= deltaTime;
        if (jitterTimer <= 0f)
        {
            seed = Random.Range(0f, 100f);
            jitterTimer = jitterInterval;
        }

        if (from != null) fromPosition = from.position;
        if (to != null) toPosition = to.position;

        Vector3 direction = (toPosition - fromPosition).normalized;
        line.SetPosition(0, fromPosition - direction * padding);
        line.SetPosition(1, toPosition + direction * padding);

        // Stays bright early, fades faster near the end.
        float progress = elapsed / duration;
        line.startColor = line.endColor = new Color(1f, 1f, 1f, 1f - progress * progress);

        line.GetPropertyBlock(block);
        block.SetVector(StartId, fromPosition);
        block.SetVector(EndId, toPosition);
        block.SetFloat(SeedId, seed);
        line.SetPropertyBlock(block);
    }
}
