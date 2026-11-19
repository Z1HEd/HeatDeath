using System.Collections.Generic;
using UnityEngine;

// A look shown on a ship while an effect referencing this prefab is on it (Effect.Visual): a stun's sparks, a slow's
// streaks. ShipAppearance keeps one instance per prefab as a child of the ship and calls End once no effect references
// it. It draws its material on sprites placed where the look belongs, fades in, and after End fades out and removes
// itself. Its shader gets the fade as the sprite's alpha and the ship's velocity as _Velocity. Other components on the
// prefab (ThrusterCut) act while it's there.
public class StatusVisual : MonoBehaviour
{
    public enum Placement { Hull, AroundShip, Weapons }

    private static readonly int VelocityId = Shader.PropertyToID("_Velocity");
    private static Sprite squareSprite;

    [Tooltip("Empty for a look drawn only by other components on the prefab (ShockSparks).")]
    [SerializeField] private Material material;
    [Tooltip("Hull: a copy of the ship's sprite. AroundShip: a square around the ship. Weapons: a copy of each " +
             "weapon's sprite, including weapons added later.")]
    [SerializeField] private Placement placement = Placement.Hull;
    [Tooltip("AroundShip: the square's side as a share of the hull's longest side.")]
    [SerializeField] private float aroundSize = 1.6f;
    [Tooltip("Hull: the copy's size relative to the ship's sprite (the shield is drawn at 1.12).")]
    [SerializeField] private float hullScale = 1f;
    [SerializeField] private float fadeTime = 0.15f;
    [Tooltip("Sorting order above the sprite it covers.")]
    [SerializeField] private int sortingOffset = 5;

    private Ship ship;
    private Rigidbody2D body;
    private readonly List<SpriteRenderer> parts = new List<SpriteRenderer>();
    private readonly HashSet<WeaponModule> coveredWeapons = new HashSet<WeaponModule>();
    private MaterialPropertyBlock block;
    private float shown;
    private bool ending;

    public void End() => ending = true;

    private void Start()
    {
        ship = GetComponentInParent<Ship>();
        if (ship == null)
        {
            Debug.LogError($"{name}: a StatusVisual needs a ship to sit on");
            Destroy(gameObject);
            return;
        }

        body = ship.GetComponent<Rigidbody2D>();
        block = new MaterialPropertyBlock();
        if (material == null)
            return;
        var hull = ship.GetComponent<SpriteRenderer>();
        if (placement == Placement.Hull)
        {
            Cover(hull, transform);
            transform.localScale = Vector3.one * hullScale;
        }
        else if (placement == Placement.AroundShip)
            Surround(hull);
    }

    private void Update()
    {
        shown = Mathf.MoveTowards(shown, ending ? 0f : 1f, Time.deltaTime / Mathf.Max(fadeTime, 1e-3f));
        if (ending && shown <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        if (placement == Placement.Weapons && material != null)
            CoverNewWeapons();

        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
        foreach (var part in parts)
        {
            if (part == null)
                continue;
            part.color = new Color(1f, 1f, 1f, shown);
            part.GetPropertyBlock(block);
            block.SetVector(VelocityId, velocity);
            part.SetPropertyBlock(block);
        }
    }

    private void CoverNewWeapons()
    {
        foreach (var weapon in ship.moduleManager.WeaponModules)
            if (weapon != null && coveredWeapons.Add(weapon))
                Cover(weapon.GetComponent<SpriteRenderer>(), weapon.transform);
    }

    // A copy of the sprite on the object that shows it, so it follows it.
    private void Cover(SpriteRenderer source, Transform parent)
    {
        if (source != null)
            MakePart(source, parent).sprite = source.sprite;
    }

    private void Surround(SpriteRenderer hull)
    {
        if (hull == null)
            return;
        if (squareSprite == null)
            squareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

        var part = MakePart(hull, transform);
        part.sprite = squareSprite;
        part.transform.localScale = Vector3.one * Mathf.Max(hull.sprite.bounds.size.x, hull.sprite.bounds.size.y)
                                    * aroundSize;
    }

    private SpriteRenderer MakePart(SpriteRenderer source, Transform parent)
    {
        var part = new GameObject(name).AddComponent<SpriteRenderer>();
        part.transform.SetParent(parent, false);
        part.sharedMaterial = material;
        part.sortingLayerID = source.sortingLayerID;
        part.sortingOrder = source.sortingOrder + sortingOffset;
        part.color = new Color(1f, 1f, 1f, 0f);
        parts.Add(part);
        return part;
    }

    // Weapon copies sit on the weapons, not under this object.
    private void OnDestroy()
    {
        foreach (var part in parts)
            if (part != null && part.transform.parent != transform)
                Destroy(part.gameObject);
    }
}
