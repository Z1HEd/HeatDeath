using UnityEngine;

// On a StatusVisual prefab: brief electric shocks at random spots on the ship's hull, as if parts of it were
// malfunctioning. Each shock stays at its spot and flickers a few times. The stun look, shared by every stun,
// Thunder's EMP included.
public class ShockSparks : MonoBehaviour
{
    [SerializeField] private Material material;
    [Tooltip("How far a shock's sparks can reach from its spot, in world units.")]
    [SerializeField] private float regionRadius = 0.28f;
    [Tooltip("Seconds between new shocks are picked at random between this and Max Interval.")]
    [SerializeField] private float minInterval = 0.08f;
    [SerializeField] private float maxInterval = 0.3f;
    [Tooltip("Seconds for one flicker, on and off.")]
    [SerializeField] private float flickerLength = 0.09f;
    [Tooltip("Each shock flickers between 1 and this many times.")]
    [SerializeField] private int maxFlickers = 3;

    private Ship ship;
    private float timeToNextShock;
    private int sortingLayerID;

    private void Start()
    {
        ship = GetComponentInParent<Ship>();
        sortingLayerID = SortingLayer.NameToID("Turrets");
    }

    private void Update()
    {
        timeToNextShock -= Time.deltaTime;
        if (timeToNextShock > 0f || ship == null || ship.IsDead || material == null)
            return;

        timeToNextShock = Random.Range(minInterval, maxInterval);
        ShipLightningOverlay.SpawnRegional(ship, regionRadius, Random.Range(1, maxFlickers + 1), flickerLength,
            material, sortingLayerID, 10);
    }
}
