using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Déclenche des séquences de dialogue à des tours spécifiques de la course.
/// </summary>
public class RaceDialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class LapDialogue
    {
        public int triggerAtLap;
        public List<DialogueSystem.DialogueLine> lines;
    }

    [SerializeField] private List<LapDialogue> lapDialogues;

    /// <summary>
    /// Appelé par LapManager après chaque tour complété.
    /// </summary>
    public void OnLapCompleted(int lapNumber)
    {
        if (DialogueSystem.Instance == null) return;

        foreach (LapDialogue lapDialogue in lapDialogues)
        {
            if (lapDialogue.triggerAtLap == lapNumber)
            {
                DialogueSystem.Instance.PlayDialogueSequence(lapDialogue.lines);
                return;
            }
        }
    }
}
