using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpeeederController : MonoBehaviour
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        ApplyHoverForce();
        ApplyMovement();
        ApplyRotation();
        ApplyTilt();
        LimitSpeed();
    }

    private void ReadInput()
    {
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;
        
        moveInput = new Vector2(horizontal, vertical);
    }

    private void ApplyHoverForce()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, hoverHeight * 2f, groundLayer))
        {
            float distanceToGround = hit.distance;
            float hoverError = hoverHeight - distanceToGround;
            float upwardForce = hoverError * hoverForce - rb.linearVelocity.y * hoverDamping;
            
            rb.AddForce(Vector3.up * upwardForce, ForceMode.Acceleration);
        }
    }

    private void ApplyMovement()
    {
        float throttle = moveInput.y;
        
        if (throttle > 0)
        {
            rb.AddForce(transform.forward * throttle * accelerationForce, ForceMode.Acceleration);
        }
        else if (throttle < 0)
        {
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            
            if (forwardSpeed > 0.5f)
            {
                rb.AddForce(-transform.forward * brakeForce, ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(transform.forward * throttle * reverseForce, ForceMode.Acceleration);
            }
        }
    }

    private void ApplyRotation()
    {
        float turn = moveInput.x;
        
        if (Mathf.Abs(turn) > 0.01f)
        {
            float turnAmount = turn * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }

    private void ApplyTilt()
    {
        float targetTilt = -moveInput.x * tiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.fixedDeltaTime * tiltSpeed);
        
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
        return rb.linearVelocity.magnitude / maxSpeed;
    }
}
