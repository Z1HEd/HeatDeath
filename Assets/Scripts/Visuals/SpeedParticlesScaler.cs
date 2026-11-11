using UnityEngine;

public class SpeedParticlesScaler : MonoBehaviour
{
    private MovementModule movementModule;
    private ParticleSystem particles;
    // The unscaled values. Serialized so a copy made at runtime (a recoloured thruster) inherits them
    // instead of reading the already-scaled values off its particles.
    [SerializeField, HideInInspector] private float startSpeed = -1f;
    [SerializeField, HideInInspector] private float emissionRate;
    void Awake()
    {
        movementModule = GetComponentInParent<MovementModule>();
        particles = GetComponent<ParticleSystem>();
        if (startSpeed < 0f)
        {
            startSpeed = particles.main.startSpeed.constant;
            emissionRate = particles.emission.rateOverTime.constant;
        }
    }

    void FixedUpdate()
    {
        var mainModule = particles.main;
        var emission = particles.emission;
        // Against the unmodified max speed, so a speed boost doesn't thin out the particles.
        var multiplier = Mathf.Clamp(movementModule.body.linearVelocity.magnitude / movementModule.maxSpeed.BaseValue,0.5f,1f);
        mainModule.startSpeed = startSpeed *multiplier;
        emission.rateOverTime = emissionRate *multiplier;
    }
}
