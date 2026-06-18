using UnityEngine;

public class SpeedParticlesScaler : MonoBehaviour
{
    private MovementModule movementModule;
    private ParticleSystem particles;
    private float startSpeed;
    private float emissionRate;
    void Awake()
    {
        movementModule = GetComponentInParent<MovementModule>();
        particles = GetComponent<ParticleSystem>();
        startSpeed = particles.main.startSpeed.constant;
        emissionRate = particles.emission.rateOverTime.constant;
    }

    void FixedUpdate()
    {
        var mainModule = particles.main;
        var emission = particles.emission;
        var multiplier = Mathf.Clamp(movementModule.body.linearVelocity.magnitude / movementModule.maxSpeed,0.3f,1f);
        mainModule.startSpeed = startSpeed *multiplier;
        emission.rateOverTime = emissionRate *multiplier;
    }
}
