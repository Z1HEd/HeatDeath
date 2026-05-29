using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class RangeDetector : MonoBehaviour
{
    [SerializeField] private float range = 15f;
    private CircleCollider2D circleCollider;
    [SerializeField]
    private List<Ship> trackedShips = new List<Ship>();

    public System.Action<Ship> OnShipExitedRange;

    private void OnValidate()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
            col.radius = range;
    }

    public void Initialize(float range)
    {
        trackedShips.Clear();
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.offset = Vector2.zero;
        SetRadius(range);
    }

    public void SetRadius(float newRange)
    {
        range = newRange;
        if (circleCollider != null)
            circleCollider.radius = newRange;
    }

    public Ship GetClosestTarget(Transform origin)
    {
        return GetClosestTargetTo(origin.position, null);
    }

    public Ship GetClosestTargetTo(Vector2 position, ICollection<Ship> exclude)
    {
        CleanupTargets();

        Ship closest = null;
        float closestSqr = float.MaxValue;

        foreach (var ship in trackedShips)
        {
            if (ship == null)
                continue;

            if (exclude != null && exclude.Contains(ship))
                continue;

            float sqr = ((Vector2)ship.transform.position - position).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = ship;
            }
        }

        return closest;
    }

    private void CleanupTargets()
    {
        trackedShips.RemoveAll(ship => ship == null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == null)
            return;

        var ship = other.GetComponent<Ship>();
        if (ship == null)
            return;

        if (!trackedShips.Contains(ship))
            trackedShips.Add(ship);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == null)
            return;

        var ship = other.GetComponent<Ship>();
        if (ship == null)
            return;

        trackedShips.Remove(ship);
        OnShipExitedRange?.Invoke(ship);
    }
}
