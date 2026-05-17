using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère la cinématique d'intro : désactive le joueur, déplace la caméra, lance les dialogues.
/// </summary>
public class IntroCinematic : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraStartPoint;
    [SerializeField] private Transform cameraEndPoint;
    [SerializeField] private float cinematicDuration = 30f;

    [Header("Target")]
    [SerializeField] private Transform lookTarget;

    [Header("Dialogue")]
    [SerializeField] private List<DialogueSystem.DialogueLine> introDialogue;

    [Header("References")]
    [SerializeField] private SpeeederController playerController;
    [SerializeField] private SpeeederCamera speederCamera;

    private Camera mainCamera;
    private bool   isComplete = false;

    /// <summary>Retourne vrai quand la cinématique est terminée ou a été skippée.</summary>
    public bool IsComplete => isComplete;

    private void Update()
    {
        if (!isComplete && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            SkipIntro();
    }

    // Start est une coroutine pour garantir que le singleton DialogueSystem
    // est initialisé avant d'appeler PlayDialogueSequence.
    private IEnumerator Start()
    {
        mainCamera = Camera.main;

        if (playerController != null) playerController.enabled = false;
        if (speederCamera != null) speederCamera.enabled = false;

        if (cameraStartPoint != null && mainCamera != null)
        {
            mainCamera.transform.position = cameraStartPoint.position;
            mainCamera.transform.rotation = cameraStartPoint.rotation;
        }

        // Attend une frame pour que tous les Awake() soient exécutés
        // et que DialogueSystem.Instance soit assigné.
        yield return null;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogError("IntroCinematic: DialogueSystem.Instance is null after one frame. Check that the DialogueSystem GameObject is active in the scene.");
        }
        else
        {
            DialogueSystem.Instance.PlayDialogueSequence(introDialogue);
        }

        StartCoroutine(MoveCinematicCamera());
    }

    private IEnumerator MoveCinematicCamera()
    {
        float elapsed = 0f;

        while (elapsed < cinematicDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cinematicDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (mainCamera != null && cameraStartPoint != null && cameraEndPoint != null)
            {
                mainCamera.transform.position = Vector3.Lerp(
                    cameraStartPoint.position,
                    cameraEndPoint.position,
                    smoothT
                );

                if (lookTarget != null)
                {
                    Vector3 direction = lookTarget.position - mainCamera.transform.position;
                    if (direction != Vector3.zero)
                        mainCamera.transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            yield return null;
        }

        // La caméra a fini son trajet — on rend la main au joueur.
        // Les dialogues continuent de s'afficher par-dessus le jeu s'ils ne sont pas terminés.
        OnIntroComplete();
    }

    /// <summary>
    /// Skips the intro cinematic immediately and hands control back to the player.
    /// </summary>
    public void SkipIntro()
    {
        StopAllCoroutines();
        if (DialogueSystem.Instance != null) DialogueSystem.Instance.StopDialogue();
        OnIntroComplete();
    }

    private void OnIntroComplete()
    {
        isComplete = true;

        if (playerController != null) playerController.enabled = true;
        if (speederCamera != null) speederCamera.enabled = true;

        enabled = false;
    }
}
