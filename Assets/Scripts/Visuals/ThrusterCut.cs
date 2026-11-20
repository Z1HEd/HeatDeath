using UnityEngine;

// On a StatusVisual prefab: while the visual is on a ship, the ship's thruster particles stop (a stun). They come back
// when the visual is removed.
public class ThrusterCut : MonoBehaviour
{
    private Ship ship;

    private void Start() => ship = GetComponentInParent<Ship>();

    // Every frame: copies of the thrusters for a new colour (ShipAppearance) can appear at any time.
    private void Update() => SetEmission(false);

    private void OnDestroy() => SetEmission(true);

    private void SetEmission(bool enabled)
    {
        if (ship == null)
            return;
        foreach (var thruster in ship.Appearance.Thrusters)
        {
            if (thruster == null)
                continue;
            var emission = thruster.emission;
            emission.enabled = enabled;
        }
    }
}
