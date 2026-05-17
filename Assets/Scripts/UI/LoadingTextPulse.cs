using UnityEngine;
using TMPro;

/// <summary>
/// Pulses the alpha of a TextMeshProUGUI component in a smooth fade in/out loop.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LoadingTextPulse : MonoBehaviour
{
    private const float PulseSpeed = 2f;
    private const float MinAlpha = 0.2f;
    private const float MaxAlpha = 1f;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (textComponent == null) return;

        float alpha = Mathf.Lerp(MinAlpha, MaxAlpha, (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) * 0.5f);
        Color c = textComponent.color;
        c.a = alpha;
        textComponent.color = c;
    }
}
