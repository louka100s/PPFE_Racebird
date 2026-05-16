using UnityEngine;

/// <summary>
/// Son moteur 3D spatial pour les IA.
/// Le pitch et le volume suivent la vitesse de l'AIRacer.
/// Chaque instance reçoit un pitch de base légèrement randomisé pour différencier les IA.
/// Ajoute automatiquement un AudioSource si nécessaire.
/// </summary>
[RequireComponent(typeof(AIRacer))]
public class AIEngineSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip engineClip;
    [SerializeField] private float minVolume       = 0.2f;
    [SerializeField] private float maxVolume       = 0.6f;
    [SerializeField] private float minPitch        = 0.6f;
    [SerializeField] private float maxPitch        = 1.4f;
    [SerializeField] private float smoothTime      = 0.15f;
    [SerializeField] private float maxAudioDistance = 80f;

    [Header("Speed Reference")]
    [SerializeField] private float baseSpeed = 100f;

    private AudioSource audioSource;
    private AIRacer     aiRacer;

    private float currentPitch;
    private float currentVolume;

    private void Start()
    {
        aiRacer = GetComponent<AIRacer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Légère variation de pitch de base pour que chaque IA sonne différemment
        minPitch += Random.Range(-0.05f, 0.05f);

        audioSource.clip         = engineClip;
        audioSource.loop         = true;
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 1f; // 3D — positionnel dans l'espace
        audioSource.rolloffMode  = AudioRolloffMode.Linear;
        audioSource.minDistance  = 5f;
        audioSource.maxDistance  = maxAudioDistance;
        audioSource.volume       = minVolume;
        audioSource.pitch        = minPitch;

        currentPitch  = minPitch;
        currentVolume = minVolume;

        if (engineClip != null)
            audioSource.Play();
    }

    private void Update()
    {
        if (aiRacer == null || audioSource == null) return;

        float speedRatio = Mathf.Clamp01(aiRacer.GetCurrentSpeed() / baseSpeed);

        float targetPitch  = Mathf.Lerp(minPitch,  maxPitch,  speedRatio);
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRatio);

        currentPitch  = Mathf.Lerp(currentPitch,  targetPitch,  smoothTime * Time.deltaTime * 60f);
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, smoothTime * Time.deltaTime * 60f);

        audioSource.pitch  = currentPitch;
        audioSource.volume = currentVolume;
    }
}
