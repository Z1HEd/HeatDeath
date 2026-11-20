using System.Collections.Generic;
using UnityEngine;

// The ships inside a trigger circle, centred on its parent (a weapon, a missile). Its layer decides which ships count.
[RequireComponent(typeof(CircleCollider2D))]
public class RangeDetector : MonoBehaviour
{
    private CircleCollider2D circleCollider;
    // Serialized to show in the inspector.
    [SerializeField] private List<Ship> trackedShips = new List<Ship>();

    public event System.Action<Ship> OnShipExitedRange;

    // The detector under parent, made if it has none.
    public static RangeDetector FindOrCreate(Transform parent)
    {
        var detector = parent.GetComponentInChildren<RangeDetector>(true);
        if (detector == null)
        {
            detector = new GameObject("RangeDetector").AddComponent<RangeDetector>();
            detector.transform.SetParent(parent, false);
        }
        return detector;
    }

    public void Initialize(float radius)
    {
        trackedShips.Clear();
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.offset = Vector2.zero;
        SetRadius(radius);
    }

    public void SetRadius(float radius) => circleCollider.radius = radius;

    public Ship GetClosestTarget()
    {
        Ship closest = null;
        float closestSqr = float.MaxValue;
        foreach (var ship in Targets)
        {
            float sqr = (ship.transform.position - transform.position).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = ship;
            }
        }
        return closest;
    }

    // Every ship in range, destroyed ones dropped. Copy it before hitting them: a kill can change it.
    public IReadOnlyList<Ship> Targets
    {
        get
        {
            CleanupTargets();
            return trackedShips;
        }
    }

    private void CleanupTargets()
    {
        trackedShips.RemoveAll(ship => ship == null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ship = other.GetComponent<Ship>();
        if (ship != null && !trackedShips.Contains(ship))
            trackedShips.Add(ship);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var ship = other.GetComponent<Ship>();
        if (ship == null)
            return;

        trackedShips.Remove(ship);
        OnShipExitedRange?.Invoke(ship);
    }
}
