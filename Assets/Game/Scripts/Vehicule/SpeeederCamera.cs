using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeeederCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Camera Position")]
    [SerializeField] private float distanceBehind = 6f;
    [SerializeField] private float heightAbove = 3f;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("FOV Settings")]
    [SerializeField] private float baseFOV = 65f;
    [SerializeField] private float maxFOV = 88f;
    [SerializeField] private float fovChangeSpeed = 1.5f;
    
    [Header("Speed Motion Blur")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float maxMotionBlurIntensity = 0.65f;
    [SerializeField] private float motionBlurSpeedThreshold = 0.5f;
    
    [Header("Speed Chromatic Aberration")]
    [SerializeField] private float maxChromaticAberration = 0.07f;
    [SerializeField] private float chromaticSpeedThreshold = 0.75f;
    
    [Header("Turn Roll")]
    [SerializeField] private float cameraRollAngle = 2f;
    [SerializeField] private float cameraRollSpeed = 4f;
    
    [Header("Speed Shake")]
    [SerializeField] private float shakeIntensity = 0.002f;
    [SerializeField] private float shakeSpeedThreshold = 0.4f;
    
    [Header("Turn Lag")]
    [SerializeField] private float turnLagAmount = 0.15f;
    
    private Camera cam;
    private float currentFOV;
    private float currentCameraRoll = 0f;
    private float cameraRollVelocity = 0f;
    private float currentLateralLag = 0f;
    private float lateralLagVelocity = 0f;
    private Quaternion cleanRotation;
    private MotionBlur motionBlur;
    private float currentBlurIntensity = 0f;
    private ChromaticAberration chromaticAberration;
    private float currentChromatic = 0f;
    private SpeeederController speederController;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        currentFOV = baseFOV;
        cam.fieldOfView = baseFOV;
    }

    private void Start()
    {
        if (target == null)
            target = FindFirstObjectByType<SpeeederController>()?.transform;
        
        if (target != null)
            speederController = target.GetComponent<SpeeederController>();
        
        cleanRotation = transform.rotation;
        
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out motionBlur);
            if (motionBlur != null)
                motionBlur.mode.value = MotionBlurMode.CameraOnly;
            postProcessVolume.profile.TryGet(out chromaticAberration);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetForward = target.right;
        Vector3 desiredPosition = target.position - targetForward * distanceBehind + Vector3.up * heightAbove;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        Vector3 lookAtPosition = target.position + Vector3.up * (heightAbove * 0.5f);
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
        cleanRotation = Quaternion.Slerp(cleanRotation, desiredRotation, rotationSpeed * Time.deltaTime);

        Quaternion finalRotation = cleanRotation;

        if (speederController != null)
        {
            float speedRatio = speederController.GetNormalizedSpeed();
            float turnInput = speederController.GetTurnInput();
            
            float rollSpeedFactor = Mathf.Clamp01((speedRatio - 0.1f) / 0.3f);
            float targetRoll = -turnInput * cameraRollAngle * rollSpeedFactor;
            currentCameraRoll = Mathf.SmoothDamp(currentCameraRoll, targetRoll, ref cameraRollVelocity, 1f / cameraRollSpeed);
            
            finalRotation *= Quaternion.AngleAxis(currentCameraRoll, Vector3.forward);
            
            if (speedRatio > shakeSpeedThreshold)
            {
                float shakeAmount = (speedRatio - shakeSpeedThreshold) / (1f - shakeSpeedThreshold) * shakeIntensity;
                Vector3 shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * shakeAmount,
                    (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * shakeAmount,
                    0f
                );
                transform.position += transform.TransformDirection(shakeOffset);
            }
            
            float targetLag = turnInput * turnLagAmount * speedRatio;
            currentLateralLag = Mathf.SmoothDamp(currentLateralLag, targetLag, ref lateralLagVelocity, 0.15f);
            transform.position += -transform.right * currentLateralLag;
        }

        transform.rotation = finalRotation;

        UpdateFieldOfView();
    }

    private void UpdateFieldOfView()
    {
        if (speederController != null)
        {
            float speedRatio = speederController.GetNormalizedSpeed();
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, Mathf.Pow(speedRatio, 1.5f));
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovChangeSpeed * Time.deltaTime);
            cam.fieldOfView = currentFOV;
            
            if (motionBlur != null)
            {
                float speedRatioLocal = speederController.GetNormalizedSpeed();
                float targetBlur = 0f;
                if (speedRatioLocal > motionBlurSpeedThreshold)
                {
                    float blurFactor = (speedRatioLocal - motionBlurSpeedThreshold) / (1f - motionBlurSpeedThreshold);
                    targetBlur = Mathf.Pow(blurFactor, 1.5f) * maxMotionBlurIntensity;
                }
                currentBlurIntensity = Mathf.Lerp(currentBlurIntensity, targetBlur, 3f * Time.deltaTime);
                motionBlur.intensity.value = currentBlurIntensity;
                motionBlur.active = currentBlurIntensity > 0.01f;
            }
            
            if (chromaticAberration != null)
            {
                float speedRatioLocal = speederController.GetNormalizedSpeed();
                float targetChromatic = 0f;
                if (speedRatioLocal > chromaticSpeedThreshold)
                {
                    float chromFactor = (speedRatioLocal - chromaticSpeedThreshold) / (1f - chromaticSpeedThreshold);
                    targetChromatic = Mathf.Pow(chromFactor, 1.5f) * maxChromaticAberration;
                }
                currentChromatic = Mathf.Lerp(currentChromatic, targetChromatic, 3f * Time.deltaTime);
                chromaticAberration.intensity.value = currentChromatic;
                chromaticAberration.active = currentChromatic > 0.01f;
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
