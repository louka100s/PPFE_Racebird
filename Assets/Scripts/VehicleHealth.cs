using System.Collections;
using UnityEngine;

/// <summary>
/// Manages health, collision damage and destruction for both the player Speeder and AI racers.
/// Attach to the root GameObject of any vehicle that participates in the collision system.
/// </summary>
public class VehicleHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Collision Damage")]
    [SerializeField] private float minDamageSpeed = 120f;
    [SerializeField] private float maxDamage = 35f;
    [SerializeField] private float speedDifferenceMultiplier = 0.8f;
    [SerializeField] private float collisionCooldown = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject explosionPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private float     destroyVolume = 1.5f;

    private float lastCollisionTime = -10f;
    private bool  isDead            = false;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by CollisionDetector to apply speed-differential damage.
    /// The attacker's speed must exceed the defender's to deal damage.
    /// </summary>
    /// <param name="impactSpeed">Speed of the attacking vehicle (units/s).</param>
    /// <param name="otherSpeed">Speed of this vehicle (units/s).</param>
    /// <param name="contactPoint">World-space position of the contact.</param>
    public void TakeCollisionDamage(float attackerSpeed, float mySpeed, Vector3 contactPoint)
    {
        if (isDead) return;
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        float speedDiff = attackerSpeed - mySpeed;

        if (speedDiff < minDamageSpeed * 0.3f) return;

        lastCollisionTime = Time.time;

        float normalizedDiff = Mathf.Clamp01((speedDiff - minDamageSpeed * 0.3f) / minDamageSpeed);
        float damage = normalizedDiff * maxDamage * speedDifferenceMultiplier;
        damage = Mathf.Clamp(damage, 5f, maxDamage);

        currentHealth -= damage;

        if (currentHealth <= 0f)
            Explode(contactPoint);
    }

    /// <summary>Returns the vehicle's health as a 0–1 ratio.</summary>
    public float GetHealthRatio() => currentHealth / maxHealth;

    /// <summary>Returns true when this vehicle has been destroyed.</summary>
    public bool IsDead() => isDead;

    /// <summary>
    /// Returns the current horizontal speed in world units per second.
    /// For the player reads directly from the Rigidbody; for the AI reads currentSpeed.
    /// </summary>
    public float GetCurrentSpeed()
    {
        SpeeederController sc = GetComponent<SpeeederController>();
        if (sc != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                return horizontalVel.magnitude;
            }
        }

        AIRacer ai = GetComponent<AIRacer>();
        if (ai != null) return ai.GetCurrentSpeed();

        return 0f;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void Explode(Vector3 position)
    {
        isDead = true;

        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position, destroyVolume);

        // Spawn explosion VFX
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        SpeeederController playerController = GetComponent<SpeeederController>();
        AIRacer            aiRacer          = GetComponent<AIRacer>();

        if (playerController != null)
        {
            // Player destroyed — disable controls and show defeat screen
            playerController.enabled = false;
            StartCoroutine(ShowDeathScreenDelayed(2f));
        }
        else if (aiRacer != null)
        {
            // AI destroyed — disable logic and hide mesh
            aiRacer.enabled = false;

            Transform visual = transform.Find("Visual");
            if (visual != null) visual.gameObject.SetActive(false);

            foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
                mr.enabled = false;

            Destroy(gameObject, 3f);
        }
    }

    private IEnumerator ShowDeathScreenDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        DeathScreen screen = FindFirstObjectByType<DeathScreen>(FindObjectsInactive.Include);
        if (screen != null)
            screen.Show();
    }
}
