using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SpeeederController : MonoBehaviour, InputAction_PlayerControl.ISpeederActions
{
    [Header("Movement Settings")]
    [SerializeField] private float accelerationForce = 50f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float brakeForce = 30f;
    [SerializeField] private float reverseForce = 20f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField] private float tiltSpeed = 3f;
    
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
    private float currentTilt;
    private InputAction_PlayerControl controls;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        
        controls = new InputAction_PlayerControl();
        controls.Speeder.SetCallbacks(this);
        
        Debug.Log("[Speeder] InputAction_PlayerControl initialisé");
    }

    private void Start()
    {
        Debug.Log($"[Speeder] Démarrage - Position: {transform.position}, Rotation: {transform.rotation.eulerAngles}");
        Debug.Log($"[Speeder] Forward: {transform.forward}");
        Debug.Log($"[Speeder] Right: {transform.right}");
        Debug.Log($"[Speeder] Back: {-transform.forward}");
        Debug.Log($"[Speeder] Left: {-transform.right}");
        Debug.Log($"[Speeder] Up: {transform.up}");
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
        ApplyTilt();
        LimitSpeed();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        
        if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
        {
            Debug.Log($"[Speeder OnMove] Input reçu - X (steer): {moveInput.x:F2}, Y (throttle): {moveInput.y:F2}");
        }
    }

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

    private void ApplyMovement()
    {
        // INVERSION: Z doit avancer (+1) et S reculer (-1)
        float throttle = -moveInput.y;
        
        // FIX: Utiliser -right car right pointe vers l'arrière (Z-)
        Vector3 moveDirection = -transform.right;
        
        if (Mathf.Abs(throttle) > 0.1f)
        {
            Debug.Log($"[ApplyMovement] throttle={throttle:F2}, moveDir={moveDirection}, speed={rb.linearVelocity.magnitude:F2}");
        }
        
        if (throttle > 0f)
        {
            rb.AddForce(moveDirection * throttle * accelerationForce, ForceMode.Acceleration);
        }
        else if (throttle < 0f)
        {
            float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, moveDirection);
            if (currentForwardSpeed > 0.5f)
            {
                rb.AddForce(-moveDirection * brakeForce, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(moveDirection * throttle * reverseForce, ForceMode.Acceleration);
            }
        }
    }

    private void ApplyRotation()
    {
        float horizontal = moveInput.x;
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // Permettre rotation même à faible vitesse, mais augmenter avec la vitesse
        float speedFactor = Mathf.Clamp01(0.3f + (currentSpeed / 10f));
        float turnAmount = horizontal * turnSpeed * speedFactor * Time.fixedDeltaTime;
        
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            Debug.Log($"[Rotation] horizontal={horizontal:F2}, speed={currentSpeed:F2}, speedFactor={speedFactor:F2}, turnAmount={turnAmount:F2}");
        }
        
        // Tourner autour de l'axe UP du véhicule, pas l'axe Y mondial
        Quaternion deltaRotation = Quaternion.AngleAxis(turnAmount, transform.up);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private void ApplyTilt()
    {
        float targetTilt = -moveInput.x * tiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.fixedDeltaTime);
        
        Vector3 currentEuler = transform.localEulerAngles;
        currentEuler.z = currentTilt;
        transform.localEulerAngles = currentEuler;
    }

    private void LimitSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    public float GetNormalizedSpeed()
    {
        return Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
    }
}
