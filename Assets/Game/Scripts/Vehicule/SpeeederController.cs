using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SpeeederController : MonoBehaviour, InputAction_PlayerControl.ISpeederActions
{
    [Header("Movement Settings")]
    [SerializeField] private float accelerationForce = 200f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float brakeForce = 80f;
    [SerializeField] private float reverseForce = 30f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float minTurnSpeedFactor = 0.05f;
    [SerializeField] private float maxTurnSpeedFactor = 1f;
    [SerializeField] private float turnSpeedCurve = 0.1f;
    [SerializeField] private float rotationSmoothing = 5f;
    
    [Header("Hover Settings")]
    [SerializeField] private float hoverHeight = 2f;
    [SerializeField] private float hoverForce = 50f;
    [SerializeField] private float hoverDamping = 5f;
    [SerializeField] private LayerMask groundLayer = -1;
    
    [Header("Physics Settings")]
    [SerializeField] private float drag = 0.5f;
    [SerializeField] private float angularDrag = 3f;
    
    [Header("Drift Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float lateralGrip = 0.75f;
    [SerializeField] private float driftForce = 45f;
    [SerializeField] private float minDriftSpeed = 5f;
    
    [Header("Visual Roll")]
    [SerializeField] private float maxRollAngle = 20f;
    [SerializeField] private float maxPitchAngle = 8f;
    [SerializeField] private float maxVisualYaw = 12f;
    [SerializeField] private float maxDiagonalTwist = 6f;
    [SerializeField] private Transform visualTransform;
    
    [Header("Acceleration Pitch")]
    [SerializeField] private float accelPitchAngle = 5f;
    [SerializeField] private float brakePitchAngle = 8f;
    [SerializeField] private float accelPitchSpeedThreshold = 0.2f;
    [SerializeField] private float accelPitchSmoothTime = 0.2f;
    
    private Rigidbody rb;
    private Vector2 moveInput;
    private InputAction_PlayerControl controls;
    private float currentAngularVelocity;
    private float currentRollAngle = 0f;
    private float currentPitchAngle = 0f;
    private float currentVisualYaw = 0f;
    private float currentDiagonalTwist = 0f;
    private float currentAccelPitch = 0f;
    private float accelPitchVelocity = 0f;
    private float rollVelocity = 0f;
    private float pitchVelocity = 0f;
    private float yawVelocity = 0f;
    private float diagonalVelocity = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        
        controls = new InputAction_PlayerControl();
        controls.Speeder.SetCallbacks(this);
    }

    private void OnEnable()
    {
        controls.Speeder.Enable();
    }

    private void OnDisable()
    {
        controls.Speeder.Disable();
    }

    private void OnDestroy()
    {
        controls.Dispose();
    }

    private void FixedUpdate()
    {
        ApplyHoverForce();
        ApplyMovement();
        ApplyRotation();
        ApplyDrift();
        LimitSpeed();
        ApplyVisualRoll();
    }

    /// <summary>
    /// Callback appelé par l'Input System lors d'un mouvement
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Applique la force de sustentation pour maintenir le speeder en l'air
    /// </summary>
    private void ApplyHoverForce()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, hoverHeight * 2f, groundLayer))
        {
            float distanceRatio = hit.distance / hoverHeight;
            float error = 1f - distanceRatio;
            float force = hoverForce * Mathf.Sign(error) * Mathf.Pow(Mathf.Abs(error), 1.4f);
            Vector3 dampingForce = -rb.linearVelocity.y * Vector3.up * hoverDamping;
            rb.AddForce((Vector3.up * force + dampingForce), ForceMode.Acceleration);
            
            float hoverNoise = Mathf.Sin(Time.time * 3.5f) * 0.04f + Mathf.Sin(Time.time * 5.7f) * 0.02f;
            rb.AddForce(Vector3.up * hoverNoise, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Applique le mouvement avant/arrière au speeder
    /// Force appliquée uniquement dans la direction forward horizontale
    /// </summary>
    private void ApplyMovement()
    {
        float throttle = moveInput.y;
        
        Vector3 forward = transform.right;
        forward.y = 0f;
        forward.Normalize();
        
        if (throttle > 0f)
        {
            rb.AddForce(forward * throttle * accelerationForce, ForceMode.Acceleration);
        }
        else if (throttle < 0f)
        {
            float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, forward);
            if (currentForwardSpeed > 0.5f)
            {
                rb.AddForce(-forward * brakeForce, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(forward * throttle * reverseForce, ForceMode.Acceleration);
            }
        }
        
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 forwardVelocity = Vector3.Project(horizontalVelocity, forward);
        Vector3 lateralVelocity = horizontalVelocity - forwardVelocity;
        
        float lateralKill = 1f - (lateralGrip * Time.fixedDeltaTime * 10f);
        lateralVelocity *= Mathf.Clamp01(lateralKill);
        
        rb.linearVelocity = forwardVelocity + lateralVelocity + Vector3.up * rb.linearVelocity.y;
    }

    /// <summary>
    /// Applique la rotation gauche/droite (yaw uniquement sur l'axe Y)
    /// Rotation progressive avec inertie pour un comportement de speeder flottant
    /// Intensité liée à la vitesse : très faible à l'arrêt, forte en mouvement
    /// </summary>
    private void ApplyRotation()
    {
        float horizontal = moveInput.x;
        
        // Calcul de la vitesse horizontale pour éviter l'influence des mouvements verticaux
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        
        // Facteur de vitesse : progression exponentielle douce
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
        float speedFactor = Mathf.Lerp(minTurnSpeedFactor, maxTurnSpeedFactor, 
                                       Mathf.Pow(normalizedSpeed, turnSpeedCurve));
        
        // Vélocité angulaire cible basée sur l'input et la vitesse
        float targetAngularVelocity = horizontal * turnSpeed * speedFactor;
        
        // Interpolation progressive vers la vélocité angulaire cible (damping/smoothing)
        bool isAccelerating = Mathf.Abs(targetAngularVelocity) > Mathf.Abs(currentAngularVelocity) + 0.5f;
        float smoothRate = isAccelerating
            ? rotationSmoothing * 1.2f
            : rotationSmoothing * 0.6f;
        currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, targetAngularVelocity, 
                                            smoothRate * Time.fixedDeltaTime);
        
        // Application de la rotation via la vélocité angulaire lissée
        if (Mathf.Abs(currentAngularVelocity) > 0.01f)
        {
            float rotationThisFrame = currentAngularVelocity * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationThisFrame, Vector3.up);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    /// <summary>
    /// Applique le système de drift latéral basé sur l'angle entre direction et vélocité
    /// </summary>
    private void ApplyDrift()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (currentSpeed < minDriftSpeed) return;

        Vector3 forward = transform.right;
        forward.y = 0f;
        forward.Normalize();

        Vector3 velocityDirection = horizontalVelocity.normalized;
        float driftAngle = Vector3.SignedAngle(forward, velocityDirection, Vector3.up);

        if (Mathf.Abs(driftAngle) < 1f) return;

        Vector3 lateralDirection = -transform.forward;
        lateralDirection.y = 0f;
        lateralDirection.Normalize();

        float driftIntensity = Mathf.Clamp01(Mathf.Abs(driftAngle) / 90f);
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        float gripForce = driftIntensity * driftForce * speedRatio * lateralGrip;

        Vector3 gripDirection = -Mathf.Sign(driftAngle) * lateralDirection;
        rb.AddForce(gripDirection * gripForce, ForceMode.Acceleration);
        
        float driftDragFactor = 1f - (driftIntensity * speedRatio * 0.15f * Time.fixedDeltaTime);
        Vector3 hVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        hVel *= Mathf.Clamp01(driftDragFactor);
        rb.linearVelocity = new Vector3(hVel.x, rb.linearVelocity.y, hVel.z);
    }

    /// <summary>
    /// Applique l'inclinaison visuelle (roll) au mesh en fonction de l'input de rotation
    /// </summary>
    private void ApplyVisualRoll()
    {
        if (visualTransform == null) return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

        float visualFactor = Mathf.Pow(speedRatio, 2.0f);

        float turnInput = moveInput.x;

        float targetRoll = -turnInput * maxRollAngle * visualFactor;
        currentRollAngle = Mathf.SmoothDamp(currentRollAngle, targetRoll, ref rollVelocity, 0.12f);

        float targetPitch = Mathf.Abs(turnInput) * maxPitchAngle * visualFactor;
        if (turnInput < 0)
            targetPitch = -targetPitch;
        currentPitchAngle = Mathf.SmoothDamp(currentPitchAngle, targetPitch, ref pitchVelocity, 0.25f);

        float targetYaw = -turnInput * maxVisualYaw * visualFactor;
        if (turnInput < 0)
            targetYaw = -targetYaw;
        currentVisualYaw = Mathf.SmoothDamp(currentVisualYaw, targetYaw, ref yawVelocity, 0.25f);

        float targetDiagonalTwist = turnInput * maxDiagonalTwist * visualFactor;
        currentDiagonalTwist = Mathf.SmoothDamp(currentDiagonalTwist, targetDiagonalTwist, ref diagonalVelocity, 0.15f);

        float accelInput = moveInput.y;
        float accelPitchSpeedFactor = Mathf.Clamp01(currentSpeed / (maxSpeed * accelPitchSpeedThreshold));
        float targetAccelPitch = 0f;
        if (accelInput > 0.01f)
            targetAccelPitch = -accelInput * accelPitchAngle * accelPitchSpeedFactor;
        else if (accelInput < -0.01f)
            targetAccelPitch = -accelInput * brakePitchAngle * accelPitchSpeedFactor;
        currentAccelPitch = Mathf.SmoothDamp(currentAccelPitch, targetAccelPitch, ref accelPitchVelocity, accelPitchSmoothTime);

        Quaternion rollRot = Quaternion.AngleAxis(currentRollAngle, Vector3.right);
        Quaternion pitchRot = Quaternion.AngleAxis(currentPitchAngle, Vector3.up);
        Quaternion yawRot = Quaternion.AngleAxis(currentVisualYaw, Vector3.forward);
        Quaternion diagonalRot = Quaternion.AngleAxis(currentDiagonalTwist, Vector3.back);

        Quaternion accelPitchRot = Quaternion.AngleAxis(currentAccelPitch, Vector3.forward);
        visualTransform.localRotation = pitchRot * yawRot * rollRot * diagonalRot * accelPitchRot;
    }

    /// <summary>
    /// Limite la vitesse maximale du speeder
    /// </summary>
    private void LimitSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    /// <summary>
    /// Retourne la vitesse normalisée du speeder (0-1)
    /// </summary>
    public float GetNormalizedSpeed()
    {
        return Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
    }
}
