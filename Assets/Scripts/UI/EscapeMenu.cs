using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu Échap superposé en ScreenSpaceOverlay.
/// Gère le volume de la musique via un slider.
/// Sera étendu avec d'autres options de menu.
/// Se crée son propre Canvas en code — aucun prefab requis.
/// </summary>
public class EscapeMenu : MonoBehaviour
{
    private const string CANVAS_NAME    = "EscapeMenuCanvas";
    private const int    CANVAS_ORDER   = 100;
    private const float  PANEL_WIDTH    = 400f;
    private const float  PANEL_HEIGHT   = 260f;

    private bool isOpen = false;

    private GameObject menuPanel;
    private Slider     volumeSlider;
    private TextMeshProUGUI volumeLabel;

    private void Start()
    {
        BuildUI();
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleMenu();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Ouvre ou ferme le menu.</summary>
    public void ToggleMenu()
    {
        SetMenuVisible(!isOpen);
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private void SetMenuVisible(bool visible)
    {
        isOpen = visible;
        menuPanel.SetActive(visible);
        Time.timeScale = visible ? 0f : 1f;
        Cursor.visible   = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;

        if (visible)
            SyncSliderToManager();
    }

    private void SyncSliderToManager()
    {
        if (MusicManager.Instance != null && volumeSlider != null)
            volumeSlider.value = MusicManager.Instance.Volume;
    }

    private void OnVolumeChanged(float value)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.Volume = value;

        if (volumeLabel != null)
            volumeLabel.text = $"Musique  {Mathf.RoundToInt(value * 100f)} %";
    }

    private void OnResumeClicked()
    {
        SetMenuVisible(false);
    }

    // -------------------------------------------------------------------------
    // UI construction
    // -------------------------------------------------------------------------

    private void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject(CANVAS_NAME);
        DontDestroyOnLoad(canvasObj);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CANVAS_ORDER;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Panel centré semi-transparent
        menuPanel = CreateElement("Panel", canvasObj.transform);
        RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRect.pivot            = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta        = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

        Image panelBg = menuPanel.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.04f, 0.12f, 0.92f);

        // Titre
        GameObject titleObj = CreateElement("Title", menuPanel.transform);
        SetupRect(titleObj, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -50f), new Vector2(0f, 50f));
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text      = "MENU";
        title.fontSize  = 36f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = Color.white;

        // Label volume
        GameObject labelObj = CreateElement("VolumeLabel", menuPanel.transform);
        SetupRect(labelObj, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -110f), new Vector2(0f, 30f));
        volumeLabel = labelObj.AddComponent<TextMeshProUGUI>();
        volumeLabel.text      = "Musique  70 %";
        volumeLabel.fontSize  = 22f;
        volumeLabel.alignment = TextAlignmentOptions.Left;
        volumeLabel.color     = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Slider volume
        GameObject sliderObj = CreateElement("VolumeSlider", menuPanel.transform);
        SetupRect(sliderObj, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -155f), new Vector2(0f, 30f));

        volumeSlider = BuildSlider(sliderObj);
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value    = MusicManager.Instance != null ? MusicManager.Instance.Volume : 0.7f;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // Bouton Reprendre
        GameObject resumeBtn = BuildButton("BtnResume", menuPanel.transform, "REPRENDRE");
        SetupRect(resumeBtn, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                  new Vector2(0f, 45f), new Vector2(220f, 50f));
        resumeBtn.GetComponent<Button>().onClick.AddListener(OnResumeClicked);
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    private static GameObject CreateElement(string elementName, Transform parent)
    {
        GameObject obj = new GameObject(elementName);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void SetupRect(GameObject obj,
                                   Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                   Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rt   = obj.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
    }

    private static Slider BuildSlider(GameObject parent)
    {
        Slider slider = parent.AddComponent<Slider>();

        // Background
        GameObject bg = CreateElement("Background", parent.transform);
        SetupRect(bg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 0f));
        Image bgImg  = bg.AddComponent<Image>();
        bgImg.color  = new Color(0.15f, 0.15f, 0.25f, 1f);
        slider.targetGraphic = bgImg;

        // Fill area
        GameObject fillArea = CreateElement("Fill Area", parent.transform);
        SetupRect(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f),
                  new Vector2(0.5f, 0.5f), new Vector2(-5f, 0f), new Vector2(-20f, 0f));

        GameObject fill = CreateElement("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg  = fill.AddComponent<Image>();
        fillImg.color  = new Color(0.2f, 0.6f, 1f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        // Handle area
        GameObject handleArea = CreateElement("Handle Slide Area", parent.transform);
        SetupRect(handleArea, Vector2.zero, Vector2.one,
                  new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));

        GameObject handle = CreateElement("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.sizeDelta = new Vector2(20f, 0f);
        Image handleImg  = handle.AddComponent<Image>();
        handleImg.color  = Color.white;
        slider.handleRect = handle.GetComponent<RectTransform>();

        return slider;
    }

    private static GameObject BuildButton(string elementName, Transform parent, string label)
    {
        GameObject obj = CreateElement(elementName, parent);

        Image bg   = obj.AddComponent<Image>();
        bg.color   = new Color(0.2f, 0.6f, 1f, 0.9f);

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.75f, 1f, 1f);
        cb.pressedColor     = new Color(0.1f, 0.45f, 0.8f, 1f);
        btn.colors          = cb;

        GameObject textObj = CreateElement("Label", obj.transform);
        RectTransform tr   = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 22f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;

        return obj;
    }
}
