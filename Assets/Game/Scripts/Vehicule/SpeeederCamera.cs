using UnityEngine;

public class SpeeederCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Camera Position")]
    [SerializeField] private float distanceBehind = 8f;
    [SerializeField] private float heightAbove = 3f;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("FOV Settings")]
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOV = 75f;
    [SerializeField] private float fovChangeSpeed = 3f;
    
    private Camera cam;
    private float currentFOV;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        currentFOV = baseFOV;
        cam.fieldOfView = baseFOV;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetForward = -target.right;
        Vector3 desiredPosition = target.position - targetForward * distanceBehind + Vector3.up * heightAbove;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        Vector3 lookAtPosition = target.position + Vector3.up * (heightAbove * 0.5f);
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

        UpdateFieldOfView();
    }

    private void UpdateFieldOfView()
    {
        SpeeederController controller = target.GetComponent<SpeeederController>();
        if (controller != null)
        {
            float speedRatio = controller.GetNormalizedSpeed();
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio);
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
            cam.fieldOfView = currentFOV;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
