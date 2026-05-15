using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Affiche un faux compteur de vitesse en bas à gauche de l'écran.
/// Crée son propre Canvas (sortOrder bas) pour ne pas interférer avec les particules de speed lines.
/// </summary>
public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeeederController speederController;

    [Header("Fake Speed Settings")]
    [Tooltip("Multiplicateur appliqué à la vitesse réelle pour obtenir une valeur affichée impressionnante.")]
    [SerializeField] private float speedMultiplier = 6.5f;

    [Tooltip("Vitesse affichée maximale en km/h.")]
    [SerializeField] private float maxDisplaySpeed = 950f;

    [Tooltip("Vitesse de lissage pour l'affichage (évite les sauts brutaux).")]
    [SerializeField] private float displaySmoothing = 8f;

    [Header("Noise")]
    [Tooltip("Amplitude du bruit ajouté pour un rendu plus organique.")]
    [SerializeField] private float noiseAmplitude = 12f;

    [Tooltip("Fréquence du bruit Perlin.")]
    [SerializeField] private float noiseFrequency = 4f;

    [Header("Visual")]
    [Tooltip("Taille de la police du chiffre de vitesse.")]
    [SerializeField] private float speedFontSize = 72f;

    [Tooltip("Taille de la police de l'unité km/h.")]
    [SerializeField] private float unitFontSize = 24f;

    [Tooltip("Couleur principale du compteur.")]
    [SerializeField] private Color speedColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Couleur quand la vitesse dépasse 80% du max.")]
    [SerializeField] private Color highSpeedColor = new Color(1f, 0.4f, 0.3f, 1f);

    private const string CANVAS_NAME = "SpeedometerCanvas";
    private const float HIGH_SPEED_THRESHOLD = 0.8f;
    private const float COLOR_LERP_SPEED = 5f;

    private TextMeshProUGUI speedText;
    private TextMeshProUGUI unitText;
    private float displayedSpeed;
    private float currentColorLerp;

    private void Start()
    {
        if (speederController == null)
            speederController = FindFirstObjectByType<SpeeederController>();

        CreateUI();
    }

    private void LateUpdate()
    {
        if (speederController == null || speedText == null) return;

        float rawSpeed = speederController.GetCurrentSpeed();
        float fakeSpeed = rawSpeed * speedMultiplier;

        float speedRatio = speederController.GetNormalizedSpeed();
        float noise = 0f;
        if (speedRatio > 0.1f)
        {
            noise = (Mathf.PerlinNoise(Time.time * noiseFrequency, 0.5f) - 0.5f) * 2f
                    * noiseAmplitude * speedRatio;
        }

        float targetSpeed = Mathf.Clamp(fakeSpeed + noise, 0f, maxDisplaySpeed);
        displayedSpeed = Mathf.Lerp(displayedSpeed, targetSpeed, displaySmoothing * Time.deltaTime);

        int displayValue = Mathf.RoundToInt(displayedSpeed);
        speedText.text = displayValue.ToString();

        float highSpeedFactor = Mathf.Clamp01((speedRatio - HIGH_SPEED_THRESHOLD) / (1f - HIGH_SPEED_THRESHOLD));
        currentColorLerp = Mathf.Lerp(currentColorLerp, highSpeedFactor, COLOR_LERP_SPEED * Time.deltaTime);
        speedText.color = Color.Lerp(speedColor, highSpeedColor, currentColorLerp);
    }

    /// <summary>
    /// Crée le Canvas et les éléments UI de façon programmatique
    /// </summary>
    private void CreateUI()
    {
        // Canvas dédié avec un sortOrder bas pour ne pas gêner les particules
        GameObject canvasObj = new GameObject(CANVAS_NAME);
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Conteneur principal ancré en bas à gauche
        GameObject container = CreateUIElement("SpeedContainer", canvasObj.transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(0f, 0f);
        containerRect.pivot = new Vector2(0f, 0f);
        containerRect.anchoredPosition = new Vector2(40f, 30f);
        containerRect.sizeDelta = new Vector2(220f, 100f);

        // Texte du chiffre de vitesse
        GameObject speedObj = CreateUIElement("SpeedValue", container.transform);
        speedText = speedObj.AddComponent<TextMeshProUGUI>();
        speedText.text = "0";
        speedText.fontSize = speedFontSize;
        speedText.fontStyle = FontStyles.Bold;
        speedText.color = speedColor;
        speedText.alignment = TextAlignmentOptions.BottomLeft;
        speedText.enableAutoSizing = false;

        RectTransform speedRect = speedObj.GetComponent<RectTransform>();
        speedRect.anchorMin = Vector2.zero;
        speedRect.anchorMax = Vector2.one;
        speedRect.offsetMin = Vector2.zero;
        speedRect.offsetMax = new Vector2(-50f, 0f);

        // Texte de l'unité "km/h"
        GameObject unitObj = CreateUIElement("SpeedUnit", container.transform);
        unitText = unitObj.AddComponent<TextMeshProUGUI>();
        unitText.text = "km/h";
        unitText.fontSize = unitFontSize;
        unitText.fontStyle = FontStyles.Normal;
        unitText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        unitText.alignment = TextAlignmentOptions.BottomRight;

        RectTransform unitRect = unitObj.GetComponent<RectTransform>();
        unitRect.anchorMin = new Vector2(1f, 0f);
        unitRect.anchorMax = new Vector2(1f, 0f);
        unitRect.pivot = new Vector2(1f, 0f);
        unitRect.anchoredPosition = new Vector2(0f, 5f);
        unitRect.sizeDelta = new Vector2(60f, 30f);
    }

    private GameObject CreateUIElement(string elementName, Transform parent)
    {
        GameObject obj = new GameObject(elementName);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
}
