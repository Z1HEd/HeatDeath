using UnityEngine;

// Lightning over one ship's hull, drawn by the Custom/ShipLightning shader on a copy of the
// ship's sprite. A child of the ship, so it follows it. Re-rolls the strands every
// jitterInterval, fades out over its duration, then destroys itself.
[RequireComponent(typeof(SpriteRenderer))]
public class ShipLightningOverlay : MonoBehaviour
{
    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    private SpriteRenderer overlayRenderer;
    private Material material;
    private float duration;
    private float jitterInterval;
    private float elapsed;
    private float jitterTimer;
    private Vector3 localCenter;
    private float flickerLength; // 0 = steady, fading out
    private int lastFlicker = -1;

    private const float FLICKER_ON_SHARE = 0.55f;

    // Lightning across the whole hull.
    public static ShipLightningOverlay Spawn(Ship target, float duration, float jitterInterval,
        Material material, int sortingLayerID, int sortingOrder)
    {
        var overlay = new GameObject("ShipLightning").AddComponent<ShipLightningOverlay>();
        overlay.Setup(target, Vector2.zero, 0f, duration, jitterInterval, 0f, material, sortingLayerID, sortingOrder);
        return overlay;
    }

    // A small shock at a random spot on the hull: flickers there the given number of times, then ends.
    public static ShipLightningOverlay SpawnRegional(Ship target, float regionRadius, int flickers,
        float flickerLength, Material material, int sortingLayerID, int sortingOrder)
    {
        Vector2 spot = RandomPointOn(target.GetComponent<SpriteRenderer>().sprite);
        var overlay = new GameObject("ShipLightning").AddComponent<ShipLightningOverlay>();
        overlay.Setup(target, spot, regionRadius, flickers * flickerLength, 0f, flickerLength,
            material, sortingLayerID, sortingOrder);
        return overlay;
    }

    // Picks from the sprite's mesh so the spot is on the hull, not on a transparent corner.
    // Weighted by triangle area, so no part of the hull is favoured.
    private static Vector2 RandomPointOn(Sprite sprite)
    {
        Vector2[] vertices = sprite.vertices;
        ushort[] triangles = sprite.triangles;

        int chosen = 0;
        float totalArea = 0f;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector2 a = vertices[triangles[i]];
            Vector2 ab = vertices[triangles[i + 1]] - a;
            Vector2 ac = vertices[triangles[i + 2]] - a;
            float area = Mathf.Abs(ab.x * ac.y - ab.y * ac.x);

            totalArea += area;
            if (Random.value * totalArea < area)
                chosen = i;
        }

        float u = Mathf.Sqrt(Random.value);
        float v = Random.value;
        return (1f - u) * vertices[triangles[chosen]]
             + u * (1f - v) * vertices[triangles[chosen + 1]]
             + u * v * vertices[triangles[chosen + 2]];
    }

    // regionRadius 0 means the whole hull.
    private void Setup(Ship target, Vector2 localCenter, float regionRadius, float duration, float jitterInterval,
        float flickerLength, Material sourceMaterial, int sortingLayerID, int sortingOrder)
    {
        this.localCenter = localCenter;
        this.flickerLength = flickerLength;
        this.duration = Mathf.Max(duration, 0.01f);
        this.jitterInterval = jitterInterval;
        transform.SetParent(target.transform, false);

        // Own copy: sprites with different textures must not share a material.
        material = Instantiate(sourceMaterial);

        overlayRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.sprite = target.GetComponent<SpriteRenderer>().sprite;
        overlayRenderer.sharedMaterial = material;
        overlayRenderer.sortingLayerID = sortingLayerID;
        overlayRenderer.sortingOrder = sortingOrder;

        material.SetFloat(RadiusId, regionRadius > 0f ? regionRadius : overlayRenderer.bounds.extents.magnitude);

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
        material.SetVector(CenterId, transform.TransformPoint(localCenter));

        if (flickerLength > 0f)
            TickFlicker();
        else
            TickSteady(deltaTime);
    }

    // The shape keeps changing while it fades: bright early, faster near the end, like LightningBolt.
    private void TickSteady(float deltaTime)
    {
        jitterTimer -= deltaTime;
        if (jitterTimer <= 0f)
        {
            material.SetFloat(SeedId, Random.Range(0f, 100f));
            jitterTimer = jitterInterval;
        }

        float progress = elapsed / duration;
        overlayRenderer.color = new Color(1f, 1f, 1f, 1f - progress * progress);
    }

    // Each flicker is one fixed shape, on for the first part of its length and then off.
    private void TickFlicker()
    {
        float flickers = elapsed / flickerLength;
        int current = Mathf.FloorToInt(flickers);
        if (current != lastFlicker)
        {
            lastFlicker = current;
            material.SetFloat(SeedId, Random.Range(0f, 100f));
        }

        bool isOn = flickers - current < FLICKER_ON_SHARE;
        overlayRenderer.color = new Color(1f, 1f, 1f, isOn ? 1f : 0f);
    }

    private void OnDestroy()
    {
        Destroy(material);
    }
}
