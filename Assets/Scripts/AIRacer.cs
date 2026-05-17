using UnityEngine;

/// <summary>
/// Moves a GameObject along a SplinePath circuit without physics.
/// The vehicle advances along its local X axis (transform.right), matching the player speeder convention.
/// A child visualTransform receives roll, pitch and hover bob effects.
/// </summary>
public class AIRacer : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private AIProfile profile;

    [Header("Path")]
    [SerializeField] private SplinePath splinePath;

    [Header("Speed Match")]
    [SerializeField] private float playerSpeedMatch = 0.95f;

    [Header("Speed Calibration")]
    [SerializeField] private float speedMultiplier = 1.8f;
    [SerializeField] private float maxProgressPerFrame = 0.05f;

    // Kept for profile compatibility and visual feedback scaling
    [Header("Reference Speed")]
    [SerializeField] private float baseSpeed = 100f;

    [Header("Start")]
    [SerializeField] private float startOffset = 0f;

    [Header("Hover")]
    [SerializeField] private float hoverHeight = 2.3f;

    [Header("Trajectory Variation")]
    [SerializeField] private float lateralOffset = 0f;
    [SerializeField] private float lateralNoiseScale = 0.3f;
    [SerializeField] private float lateralNoiseSpeed = 0.5f;

    [Header("Rubber Band")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float rubberBandStrength = 0.08f;
    [SerializeField] private float rubberBandRange = 40f;

    [Header("Contact")]
    [SerializeField] private float playerPushForce = 25f;
    [SerializeField] private float aiKnockbackDuration = 0.6f;
    [SerializeField] private float aiKnockbackOffset = 2.5f;
    [SerializeField] private float aiSlowdownOnHit = 0.6f;
    [SerializeField] private float contactCooldown = 0.3f;

    [Header("Proximity Separation")]
    [Tooltip("Minimum distance before the AI steers away from the player.")]
    [SerializeField] private float separationRadius = 12f;
    [Tooltip("How strongly the AI pushes itself away from the player when too close.")]
    [SerializeField] private float separationStrength = 6f;

    [Header("Fake Drift")]
    [SerializeField] private float driftCurvatureThreshold = 0.3f;
    [SerializeField] private float maxDriftOffset = 1.2f;
    [SerializeField] private float driftSmoothTime = 0.2f;
    [SerializeField] private float driftYawAngle = 12f;

    [Header("Visual Feedback")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float maxRollAngle = 18f;
    [SerializeField] private float maxPitchAngle = 5f;
    [SerializeField] private float hoverBobAmplitude = 0.06f;
    [SerializeField] private float hoverBobFrequency = 3f;
    [SerializeField] private float visualSmoothTime = 0.15f;

    private float currentProgress;
    private float currentSpeed;
    private float noiseSeed;

    private float knockbackTimer;
    private float knockbackLateralOffset;
    private float lastContactTime = -10f;

    private float currentDriftOffset;
    private float driftOffsetVelocity;
    private float currentDriftYaw;
    private float driftYawVelocity;

    private float currentRoll;
    private float currentPitch;
    private float rollVelocity;
    private float pitchVelocity;
    private float previousSpeed;

    private void Start()
    {
        if (profile != null)
        {
            baseSpeed         = profile.baseSpeed;
            lateralNoiseScale = profile.lateralVariation;
            maxRollAngle     *= profile.rollMultiplier;
            maxPitchAngle    *= profile.pitchMultiplier;
        }

        currentProgress = startOffset;
        currentSpeed    = 0f;
        noiseSeed       = Random.Range(0f, 1000f);

        if (playerTransform == null)
        {
            SpeeederController player = FindFirstObjectByType<SpeeederController>();
            if (player != null)
                playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (splinePath == null) return;

        float totalLength = splinePath.GetTotalLength();
        if (totalLength <= 0f) return;

        // --- Speed: match player's real physics speed ---
        float playerSpeed = 0f;
        if (playerTransform != null)
        {
            Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 horizontalVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
                playerSpeed = horizontalVel.magnitude;
            }
        }

        // --- Rubber banding: adjusts effective match ratio based on distance/position ---
        float effectiveMatch = playerSpeedMatch;
        if (playerTransform != null)
        {
            float distance      = Vector3.Distance(transform.position, playerTransform.position);
            float distanceFactor = Mathf.Clamp01(distance / rubberBandRange);

            Vector3 splineTangent = splinePath.GetDirection(currentProgress);
            Vector3 toPlayer      = playerTransform.position - transform.position;
            float dot             = Vector3.Dot(toPlayer, splineTangent);

            float matchTarget = dot > 0f
                ? playerSpeedMatch + rubberBandStrength
                : playerSpeedMatch - rubberBandStrength;

            effectiveMatch = Mathf.Lerp(playerSpeedMatch, matchTarget, distanceFactor);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, playerSpeed * effectiveMatch, 2f * Time.deltaTime);

        // --- Movement along spline (X-forward convention) ---
        float progressDelta = Mathf.Min((currentSpeed * speedMultiplier * Time.deltaTime) / totalLength, maxProgressPerFrame);
        currentProgress += progressDelta;
        currentProgress %= 1f;

        transform.position = splinePath.GetPosition(currentProgress);
        transform.position += Vector3.up * hoverHeight;

        // --- Lateral trajectory variation ---
        Vector3 splineDir = splinePath.GetDirection(currentProgress);
        Vector3 lateral   = Vector3.Cross(splineDir, Vector3.up).normalized;
        float noise       = (Mathf.PerlinNoise(noiseSeed, Time.time * lateralNoiseSpeed) - 0.5f) * 2f;
        float offsetTotal = lateralOffset + noise * lateralNoiseScale;
        transform.position += lateral * offsetTotal;

        // --- Knockback from contact ---
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            float knockFactor = Mathf.Max(knockbackTimer, 0f) / aiKnockbackDuration;
            transform.position += lateral * (knockbackLateralOffset * knockFactor * aiKnockbackOffset);
        }

        // --- Proximity separation: steer away from player to avoid overlap ---
        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist < separationRadius && dist > 0.1f)
            {
                float pushFactor = 1f - Mathf.Clamp01(dist / separationRadius);
                Vector3 awayDir = -toPlayer.normalized;
                transform.position += awayDir * (separationStrength * pushFactor * Time.deltaTime);
            }
        }

        // --- Fake drift (curvature sampled locally for visual purposes only) ---
        Vector3 dirNowDrift   = splinePath.GetDirection(currentProgress);
        Vector3 dirAheadDrift = splinePath.GetDirection(currentProgress + 0.005f);
        float turnSignDrift   = Mathf.Sign(Vector3.Cross(dirNowDrift, dirAheadDrift).y);
        float visualCurvature = splinePath.GetCurvature(currentProgress);

        float targetDriftOffset;
        float targetDriftYaw;

        if (visualCurvature < driftCurvatureThreshold)
        {
            targetDriftOffset = 0f;
            targetDriftYaw    = 0f;
        }
        else
        {
            float driftIntensity = (visualCurvature - driftCurvatureThreshold) / (1f - driftCurvatureThreshold);
            driftIntensity      *= Mathf.Clamp01(currentSpeed / baseSpeed);
            // Body slides to the outside, nose points into the turn
            targetDriftOffset    = -turnSignDrift * driftIntensity * maxDriftOffset;
            targetDriftYaw       =  turnSignDrift * driftIntensity * driftYawAngle;
        }

        currentDriftOffset = Mathf.SmoothDamp(currentDriftOffset, targetDriftOffset, ref driftOffsetVelocity, driftSmoothTime);
        currentDriftYaw    = Mathf.SmoothDamp(currentDriftYaw,    targetDriftYaw,    ref driftYawVelocity,    driftSmoothTime);
        transform.position += lateral * currentDriftOffset;

        // Orient so transform.right points along the spline tangent
        Vector3 targetRight   = splinePath.GetDirection(currentProgress);
        Vector3 smoothedRight = Vector3.Slerp(transform.right, targetRight, 12f * Time.deltaTime).normalized;
        Vector3 newForward    = new Vector3(-smoothedRight.z, 0f, smoothedRight.x);
        transform.rotation    = Quaternion.LookRotation(newForward, Vector3.up);

        // --- Visual feedback ---
        if (visualTransform != null)
        {
            // Roll: around movement axis (local X) based on turn sign and curvature
            float speedRatio = Mathf.Clamp01(currentSpeed / baseSpeed);
            float targetRoll = turnSignDrift * visualCurvature * maxRollAngle * speedRatio;
            currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, visualSmoothTime);

            // Pitch: around lateral axis (local Z) — negative on acceleration, positive on braking
            float speedDelta  = currentSpeed - previousSpeed;
            float targetPitch = Mathf.Clamp(-speedDelta / Time.deltaTime / baseSpeed, -1f, 1f) * maxPitchAngle;
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, visualSmoothTime);

            // Hover bob: two sine layers to break regularity
            float bob = Mathf.Sin(Time.time * hoverBobFrequency) * hoverBobAmplitude
                      + Mathf.Sin(Time.time * hoverBobFrequency * 1.7f) * hoverBobAmplitude * 0.3f;

            // X-forward: roll on Euler X, drift yaw on Euler Y, pitch on Euler Z
            visualTransform.localRotation = Quaternion.Euler(currentRoll, currentDriftYaw, currentPitch);
            visualTransform.localPosition = new Vector3(0f, bob, 0f);
        }

        previousSpeed = currentSpeed;
    }

    /// <summary>Handles fake collision with the player via trigger overlap.</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastContactTime < contactCooldown) return;

        SpeeederController player = other.GetComponentInParent<SpeeederController>();
        if (player == null) return;

        lastContactTime = Time.time;

        // Push the player away with a strong impulse
        Rigidbody playerRb = other.GetComponentInParent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDirection = (player.transform.position - transform.position);
            pushDirection.y = 0f;
            pushDirection.Normalize();
            playerRb.AddForce(pushDirection * playerPushForce, ForceMode.VelocityChange);
        }

        // Knockback AI to the opposite side
        Vector3 toPlayer = (player.transform.position - transform.position);
        toPlayer.y = 0f;
        Vector3 splineDir = splinePath.GetDirection(currentProgress);
        Vector3 lateralDir = Vector3.Cross(splineDir, Vector3.up).normalized;
        knockbackLateralOffset = -Mathf.Sign(Vector3.Dot(toPlayer, lateralDir));
        knockbackTimer = aiKnockbackDuration;

        // Slow down AI on impact
        currentSpeed *= aiSlowdownOnHit;
    }

    /// <summary>Continuous repulsion while the player is inside the AI trigger.</summary>
    private void OnTriggerStay(Collider other)
    {
        SpeeederController player = other.GetComponentInParent<SpeeederController>();
        if (player == null) return;

        Rigidbody playerRb = other.GetComponentInParent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDirection = (player.transform.position - transform.position);
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude < 0.01f)
                pushDirection = transform.right;
            pushDirection.Normalize();

            // Continuous push — weaker than initial impact but persistent
            playerRb.AddForce(pushDirection * (playerPushForce * 0.6f), ForceMode.Acceleration);
        }
    }

    /// <summary>Returns the current normalized progress on the circuit (0 to 1).</summary>
    public float GetProgress() => currentProgress;

    /// <summary>Returns the current speed in world units per second.</summary>
    public float GetCurrentSpeed() => currentSpeed;
}
