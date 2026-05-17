using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

/// <summary>
/// Plays a lore intro cinematic before the main menu appears.
/// Uses the existing VideoPlayer to play the intro clip with audio,
/// then switches to the menu background loop and reveals the menu UI.
/// Skippable via the Enter key.
/// </summary>
public class MenuIntroCinematic : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip introCinematicClip;
    [SerializeField] private VideoClip menuBackgroundClip;

    [Header("Audio")]
    [SerializeField] private AudioSource menuMusic;
    [SerializeField] private float introVideoVolume = 1f;

    [Header("UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject introCinematicPanel;

    private bool isPlaying = false;
    private bool isComplete = false;

    private IEnumerator Start()
    {
        // Hide menu, show cinematic panel
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (introCinematicPanel != null)
            introCinematicPanel.SetActive(true);

        // Mute menu music during cinematic
        if (menuMusic != null)
            menuMusic.Pause();

        // Configure VideoPlayer for intro: audio enabled via Direct output
        if (videoPlayer != null && introCinematicClip != null)
        {
            videoPlayer.clip = introCinematicClip;
            videoPlayer.isLooping = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.SetDirectAudioVolume(0, introVideoVolume);
            videoPlayer.playOnAwake = false;

            videoPlayer.Prepare();

            // Wait until the video is prepared
            while (!videoPlayer.isPrepared)
                yield return null;

            // Hide the black panel so the video on Camera Far Plane is visible
            if (introCinematicPanel != null)
                introCinematicPanel.SetActive(false);

            videoPlayer.Play();
            isPlaying = true;

            // Register end callback
            videoPlayer.loopPointReached += OnIntroVideoFinished;
        }
        else
        {
            // No clip assigned — skip straight to menu
            ShowMenu();
        }
    }

    private void Update()
    {
        if (isPlaying && !isComplete)
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                SkipIntro();
        }
    }

    private void OnIntroVideoFinished(VideoPlayer vp)
    {
        vp.loopPointReached -= OnIntroVideoFinished;
        ShowMenu();
    }

    private void SkipIntro()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnIntroVideoFinished;
            videoPlayer.Stop();
        }

        ShowMenu();
    }

    private void ShowMenu()
    {
        if (isComplete) return;
        isComplete = true;
        isPlaying  = false;

        // Switch VideoPlayer to menu background loop
        if (videoPlayer != null && menuBackgroundClip != null)
        {
            videoPlayer.clip = menuBackgroundClip;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.Play();
        }

        // Hide cinematic panel, show menu
        if (introCinematicPanel != null)
            introCinematicPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);

        // Start menu music
        if (menuMusic != null)
            menuMusic.Play();

        enabled = false;
    }
}
