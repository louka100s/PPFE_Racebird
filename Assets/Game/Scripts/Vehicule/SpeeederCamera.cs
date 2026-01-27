using UnityEngine;

public class SpeeederCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -8f);
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Look Settings")]
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f);
    
    [Header("Dynamic FOV")]
    [SerializeField] private bool useDynamicFOV = true;
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOV = 80f;
    [SerializeField] private float fovChangeSpeed = 2f;
    
    private Camera cam;
    private SpeeederController speedController;
    private float currentFOV;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
        }
        
        currentFOV = baseFOV;
        cam.fieldOfView = currentFOV;
    }

    private void Start()
    {
        if (target != null)
        {
            speedController = target.GetComponent<SpeeederController>();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        
        FollowTarget();
        LookAtTarget();
        UpdateDynamicFOV();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }

    private void LookAtTarget()
    {
        Vector3 lookPosition = target.position + target.rotation * lookOffset;
        Quaternion targetRotation = Quaternion.LookRotation(lookPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateDynamicFOV()
    {
        if (!useDynamicFOV || speedController == null) return;
        
        float speedRatio = speedController.GetNormalizedSpeed();
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
        cam.fieldOfView = currentFOV;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        speedController = newTarget?.GetComponent<SpeeederController>();
    }
}
