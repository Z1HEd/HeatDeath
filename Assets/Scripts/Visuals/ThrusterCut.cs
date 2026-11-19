using UnityEngine;

// On a StatusVisual prefab: while the visual is on a ship, the ship's thruster particles stop (a stun) or cut in and
// out (a slow). They come back when the visual is removed.
public class ThrusterCut : MonoBehaviour
{
    public enum Mode { Stop, Sputter }

    [SerializeField] private Mode mode = Mode.Stop;
    [Tooltip("Sputter: seconds between rolls for whether the thrusters fire.")]
    [SerializeField] private float sputterInterval = 0.08f;
    [Tooltip("Sputter: the chance each roll that the thrusters fire.")]
    [SerializeField, Range(0f, 1f)] private float firingChance = 0.5f;

    private Ship ship;
    private float timer;
    private bool firing;

    private void Start() => ship = GetComponentInParent<Ship>();

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = sputterInterval;
            firing = mode == Mode.Sputter && Random.value < firingChance;
        }
        SetEmission(firing);
    }

    private void OnDestroy() => SetEmission(true);

    private void SetEmission(bool enabled)
    {
        if (ship == null)
            return;
        foreach (var movement in ship.moduleManager.MovementModules)
        foreach (var thruster in movement.GetComponentsInChildren<ParticleSystem>())
        {
            var emission = thruster.emission;
            emission.enabled = enabled;
        }
    }
}
