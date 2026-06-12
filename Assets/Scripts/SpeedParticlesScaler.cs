using UnityEngine;

public class SpeedParticlesScaler : MonoBehaviour
{
    private MovementModule movementModule;
    private ParticleSystem particleSystem;
    private float startSpeed;
    private float emissionRate;
    void Awake()
    {
        movementModule = GetComponentInParent<MovementModule>();
        particleSystem = GetComponent<ParticleSystem>();
        startSpeed = particleSystem.main.startSpeed.constant;
        emissionRate = particleSystem.emission.rateOverTime.constant;
    }

    void FixedUpdate()
    {
        var mainModule = particleSystem.main;
        var emission = particleSystem.emission;
        var multiplier = Mathf.Clamp(movementModule.body.linearVelocity.magnitude / movementModule.maxSpeed,0.3f,1f);
        mainModule.startSpeed = startSpeed *multiplier;
        emission.rateOverTime = emissionRate *multiplier;
    }
}
