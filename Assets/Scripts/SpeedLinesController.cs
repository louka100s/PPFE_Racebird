using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SpeedLinesController : MonoBehaviour
{
    [SerializeField] private SpeeederController speederController;

    [Header("Speed Lines Settings")]
    [SerializeField] private float speedThreshold = 0.7f;
    [SerializeField] private float maxEmissionRate = 120f;
    [SerializeField] private float emissionSmoothing = 1.5f;

    private ParticleSystem speedLinesParticles;
    private float currentEmissionRate = 0f;

    private void Start()
    {
        speedLinesParticles = GetComponent<ParticleSystem>();

        if (speederController == null)
            speederController = FindFirstObjectByType<SpeeederController>();

        ConfigureParticleSystem();
    }

    /// <summary>
    /// Configures all particle system modules to match the WipEout speed lines spec.
    /// </summary>
    private void ConfigureParticleSystem()
    {
        // Main module
        ParticleSystem.MainModule main = speedLinesParticles.main;
        main.startSpeed = -25f;
        main.startSize = 0.04f;
        main.startLifetime = 0.6f;
        main.startColor = new Color(1f, 1f, 1f, 0.25f);
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.playOnAwake = true;
        main.loop = true;

        // Emission module — rate starts at 0, driven by Update()
        ParticleSystem.EmissionModule emission = speedLinesParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        // Shape module — cone pointing toward camera
        ParticleSystem.ShapeModule shape = speedLinesParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 6f;
        shape.radiusThickness = 0f;

        // Renderer — Stretched Billboard
        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        psRenderer.velocityScale = 3f;
        psRenderer.lengthScale = 0f;

        speedLinesParticles.Play();
    }

    private void LateUpdate()
    {
        if (speederController == null || speedLinesParticles == null) return;

        float speedRatio = speederController.GetNormalizedSpeed();
        float targetEmissionRate = 0f;

        if (speedRatio > speedThreshold)
        {
            float factor = (speedRatio - speedThreshold) / (1f - speedThreshold);
            targetEmissionRate = Mathf.Pow(factor, 1.5f) * maxEmissionRate;
        }

        currentEmissionRate = Mathf.Lerp(currentEmissionRate, targetEmissionRate, emissionSmoothing * Time.deltaTime);

        ParticleSystem.EmissionModule emission = speedLinesParticles.emission;
        emission.rateOverTime = currentEmissionRate;
    }
}
