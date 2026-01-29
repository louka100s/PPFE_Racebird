using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SpeeederController : MonoBehaviour, InputAction_PlayerControl.ISpeederActions
{
    [Header("Movement Settings")]
    [SerializeField] private float accelerationForce = 80f;
    [SerializeField] private float maxSpeed = 35f;
    [SerializeField] private float brakeForce = 40f;
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
    [SerializeField] private float drag = 2f;
    [SerializeField] private float angularDrag = 3f;
    
    private Rigidbody rb;
    private Vector2 moveInput;
    private InputAction_PlayerControl controls;
    private float currentAngularVelocity;

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
        LimitSpeed();
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
            float force = (1f - distanceRatio) * hoverForce;
            Vector3 dampingForce = -rb.linearVelocity.y * Vector3.up * hoverDamping;
            rb.AddForce((Vector3.up * force + dampingForce), ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Applique le mouvement avant/arrière au speeder
    /// Force appliquée uniquement dans la direction forward horizontale
    /// </summary>
    private void ApplyMovement()
    {
        float throttle = -moveInput.y;
        
        // Direction forward du speeder, projetée sur le plan horizontal pour éviter toute force verticale
        Vector3 forwardDirection = -transform.right;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();
        
        if (throttle > 0f)
        {
            rb.AddForce(forwardDirection * throttle * accelerationForce, ForceMode.Acceleration);
        }
        else if (throttle < 0f)
        {
            float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, forwardDirection);
            if (currentForwardSpeed > 0.5f)
            {
                rb.AddForce(-forwardDirection * brakeForce, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(forwardDirection * throttle * reverseForce, ForceMode.Acceleration);
            }
        }
        
        // Réduit la vélocité latérale sur le plan horizontal uniquement
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 forwardVelocity = Vector3.Project(horizontalVelocity, forwardDirection);
        Vector3 lateralVelocity = horizontalVelocity - forwardVelocity;
        
        // Réduit 90% de la glissade latérale pour un comportement arcade
        lateralVelocity *= 0.1f;
        
        // Reconstruit la vélocité : forward + latéral réduit + Y préservé
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
        currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, targetAngularVelocity, 
                                            rotationSmoothing * Time.fixedDeltaTime);
        
        // Application de la rotation via la vélocité angulaire lissée
        if (Mathf.Abs(currentAngularVelocity) > 0.01f)
        {
            float rotationThisFrame = currentAngularVelocity * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationThisFrame, Vector3.up);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
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
