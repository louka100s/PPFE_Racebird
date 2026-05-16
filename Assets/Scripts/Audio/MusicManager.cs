using UnityEngine;

/// <summary>
/// Joue la musique de fond en boucle continue.
/// Persiste entre les scènes via DontDestroyOnLoad.
/// Le volume est contrôlable depuis n'importe où via MusicManager.Instance.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;

    [Header("Settings")]
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 0.7f;

    private AudioSource audioSource;

    /// <summary>Volume courant de la musique (0 à 1).</summary>
    public float Volume
    {
        get => audioSource != null ? audioSource.volume : defaultVolume;
        set
        {
            if (audioSource != null)
                audioSource.volume = Mathf.Clamp01(value);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.clip        = musicClip;
        audioSource.loop        = true;
        audioSource.playOnAwake = false;
        audioSource.volume      = defaultVolume;
    }

    private void Start()
    {
        if (musicClip != null && !audioSource.isPlaying)
            audioSource.Play();
    }
}
