using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Singleton qui gère l'affichage séquentiel de lignes de dialogue avec typewriter effect.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [System.Serializable]
    public class DialogueLine
    {
        public enum Speaker { MJ, Snoop }
        public Speaker speaker;
        [TextArea(2, 4)]
        public string text;
        public float displayTime = 3f;
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject mjPanel;
    [SerializeField] private GameObject snoopPanel;

    [Header("Text")]
    [SerializeField] private TMP_Text mjDialogueText;
    [SerializeField] private TMP_Text snoopDialogueText;

    [Header("Settings")]
    [SerializeField] private float textSpeed = 40f;

    private Coroutine currentDialogue;
    private bool isPlaying = false;

    // État interne du typewriter pour permettre le skip
    private bool isTyping = false;
    private bool skipRequested = false;
    private string currentFullText = string.Empty;
    private TMP_Text currentActiveText;

    private void Awake()
    {
        // Singleton dans la scène — pas de DontDestroyOnLoad pour conserver
        // les références sérialisées vers les panels de la même scène.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mjPanel != null) mjPanel.SetActive(false);
        if (snoopPanel != null) snoopPanel.SetActive(false);
    }

    /// <summary>
    /// Lance une séquence de lignes de dialogue. Appelle onComplete à la fin.
    /// </summary>
    public void PlayDialogueSequence(List<DialogueLine> lines, System.Action onComplete = null)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("DialogueSystem: PlayDialogueSequence called with null or empty list.");
            onComplete?.Invoke();
            return;
        }

        Debug.Log("DialogueSystem: Starting sequence with " + lines.Count + " lines");

        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        currentDialogue = StartCoroutine(RunSequence(lines, onComplete));
    }

    /// <summary>
    /// Saute le typewriter en cours : affiche tout le texte immédiatement.
    /// Si le texte est déjà complet, n'a pas d'effet (le timer displayTime continue).
    /// </summary>
    public void SkipLine()
    {
        if (isTyping)
            skipRequested = true;
    }

    /// <summary>Retourne vrai si une séquence est en cours.</summary>
    public bool IsPlaying() => isPlaying;

    // -------------------------------------------------------------------------
    // Coroutines privées
    // -------------------------------------------------------------------------

    private IEnumerator RunSequence(List<DialogueLine> lines, System.Action onComplete)
    {
        isPlaying = true;

        foreach (DialogueLine line in lines)
        {
            Debug.Log("DialogueSystem: " + line.speaker + " says: " + line.text);

            bool isMJ = line.speaker == DialogueLine.Speaker.MJ;

            if (isMJ)
            {
                if (snoopPanel != null) snoopPanel.SetActive(false);
                if (mjPanel != null) mjPanel.SetActive(true);
                currentActiveText = mjDialogueText;
            }
            else
            {
                if (mjPanel != null) mjPanel.SetActive(false);
                if (snoopPanel != null) snoopPanel.SetActive(true);
                currentActiveText = snoopDialogueText;
            }

            if (currentActiveText != null)
                currentActiveText.text = string.Empty;

            yield return StartCoroutine(TypewriterEffect(line.text));

            yield return new WaitForSeconds(line.displayTime);
        }

        if (mjPanel != null) mjPanel.SetActive(false);
        if (snoopPanel != null) snoopPanel.SetActive(false);

        isPlaying = false;
        currentDialogue = null;
        onComplete?.Invoke();
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        skipRequested = false;
        currentFullText = fullText;

        if (currentActiveText == null)
        {
            isTyping = false;
            yield break;
        }

        currentActiveText.text = string.Empty;
        float interval = 1f / Mathf.Max(textSpeed, 1f);

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipRequested)
            {
                currentActiveText.text = fullText;
                break;
            }

            currentActiveText.text += fullText[i];
            yield return new WaitForSeconds(interval);
        }

        isTyping = false;
        skipRequested = false;
    }
}
