using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed on the player Speeder. Detects trigger overlaps with AI vehicles,
/// exchanges collision damage via VehicleHealth and provides visual + audio feedback.
/// </summary>
[RequireComponent(typeof(VehicleHealth))]
public class CollisionDetector : MonoBehaviour
{
    [Header("Screen Flash")]
    [SerializeField] private Image screenFlashImage;

    [Header("Sound")]
    [SerializeField] private AudioClip[] crashSounds;
    [SerializeField] private AudioClip   destroySound;
    [SerializeField] private float crashVolume   = 1.5f;
    [SerializeField] private float destroyVolume = 1.5f;

    [Header("Cooldown")]
    [SerializeField] private float collisionCooldown = 0.5f;

    private float     lastCollisionTime = -10f;
    private Coroutine flashRoutine;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger detected with: " + other.gameObject.name);

        if (Time.time - lastCollisionTime < collisionCooldown) return;

        VehicleHealth otherHealth = other.GetComponentInParent<VehicleHealth>();
        if (otherHealth == null || otherHealth.IsDead()) return;

        VehicleHealth myHealth = GetComponent<VehicleHealth>();
        if (myHealth == null || myHealth.IsDead()) return;

        lastCollisionTime = Time.time;

        float mySpeed    = myHealth.GetCurrentSpeed();
        float otherSpeed = otherHealth.GetCurrentSpeed();

        Debug.Log($"COLLISION: My speed={mySpeed:F1}  Other speed={otherSpeed:F1}");

        Vector3 contactPoint = other.ClosestPoint(transform.position);

        otherHealth.TakeCollisionDamage(mySpeed,    otherSpeed, contactPoint);
        myHealth.TakeCollisionDamage   (otherSpeed, mySpeed,    contactPoint);

        if (crashSounds != null && crashSounds.Length > 0)
        {
            AudioClip clip = crashSounds[Random.Range(0, crashSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position, crashVolume);
        }

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashScreen());

        SpawnImpactParticles(contactPoint);
    }

    /// <summary>Spawns a quick burst of impact particles at the given world position.</summary>
    private void SpawnImpactParticles(Vector3 position)
    {
        GameObject fx = new GameObject("ImpactFX");
        fx.transform.position = position;

        ParticleSystem ps = fx.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startSpeed      = 8f;
        main.startSize       = 0.2f;
        main.startLifetime   = 0.3f;
        main.startColor      = Color.white;
        main.maxParticles    = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop            = false;
        main.playOnAwake     = true;
        main.stopAction      = ParticleSystemStopAction.Destroy;
        main.duration        = 0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.5f;

        var psRenderer = fx.GetComponent<ParticleSystemRenderer>();
        psRenderer.material       = new Material(Shader.Find("Particles/Standard Unlit"));
        psRenderer.material.color = Color.white;

        ps.Play();
    }

    private IEnumerator FlashScreen()
    {
        if (screenFlashImage == null) yield break;

        screenFlashImage.color = new Color(1f, 0f, 0f, 0.35f);
        screenFlashImage.gameObject.SetActive(true);

        float elapsed  = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.35f, 0f, elapsed / duration);
            screenFlashImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        screenFlashImage.color = new Color(1f, 0f, 0f, 0f);
        screenFlashImage.gameObject.SetActive(false);
    }
}
