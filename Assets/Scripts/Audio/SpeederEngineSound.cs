using UnityEngine;

/// <summary>
/// Son moteur 2D pour le Speeder joueur.
/// Le pitch et le volume suivent la vitesse normalisée du SpeeederController.
/// Ajoute automatiquement un AudioSource si nécessaire.
/// </summary>
[RequireComponent(typeof(SpeeederController))]
public class SpeederEngineSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip engineClip;
    [SerializeField] private float minVolume = 0.08f;
    [SerializeField] private float maxVolume = 0.25f;
    [SerializeField] private float minPitch  = 0.6f;
    [SerializeField] private float maxPitch  = 1.5f;
    [SerializeField] private float smoothTime = 0.15f;

    private AudioSource     audioSource;
    private SpeeederController controller;

    private float currentPitch;
    private float currentVolume;

    private void Start()
    {
        controller = GetComponent<SpeeederController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip         = engineClip;
        audioSource.loop         = true;
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0f; // 2D — toujours audible au même niveau
        audioSource.volume       = minVolume;
        audioSource.pitch        = minPitch;

        currentPitch  = minPitch;
        currentVolume = minVolume;

        if (engineClip != null)
            audioSource.Play();
    }

    private void Update()
    {
        if (controller == null || audioSource == null) return;

        float speedRatio = controller.GetNormalizedSpeed();

        float targetPitch  = Mathf.Lerp(minPitch,  maxPitch,  speedRatio);
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRatio);

        currentPitch  = Mathf.Lerp(currentPitch,  targetPitch,  smoothTime * Time.deltaTime * 60f);
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, smoothTime * Time.deltaTime * 60f);

        audioSource.pitch  = currentPitch;
        audioSource.volume = currentVolume;
    }
}
