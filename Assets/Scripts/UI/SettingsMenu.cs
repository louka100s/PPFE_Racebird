using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the settings panel: audio volume, quality, resolution and fullscreen.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject menuPanel;

    [Header("Volume")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Audio Source")]
    [SerializeField] private AudioSource menuMusic;

    private Resolution[] resolutions;

    private void Start()
    {
        SetupQualityDropdown();
        SetupResolutionDropdown();
        SetupFullscreenToggle();
        SetupVolumeSliders();
    }

    /// <summary>Opens the settings panel and hides the main menu.</summary>
    public void OpenSettings()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>Closes the settings panel and shows the main menu.</summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    /// <summary>Sets the quality level.</summary>
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    /// <summary>Sets the screen resolution.</summary>
    public void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    /// <summary>Toggles fullscreen mode.</summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    /// <summary>Sets the music volume from the slider.</summary>
    public void SetMusicVolume(float volume)
    {
        if (menuMusic != null) menuMusic.volume = volume;
    }

    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void SetupFullscreenToggle()
    {
        if (fullscreenToggle == null) return;
        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    private void SetupVolumeSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.value = 0.5f;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }
}
