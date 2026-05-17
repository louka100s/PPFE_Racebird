using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gère le comptage de tours, les chronomètres et l'UI de course.
/// Singleton accessible depuis FinishLineTrigger et d'autres systèmes.
/// </summary>
public class LapManager : MonoBehaviour
{
    public static LapManager Instance { get; private set; }

    [Header("Race Settings")]
    [SerializeField] private int totalLaps = 5;

    [Header("UI")]
    [SerializeField] private TMP_Text lapText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text bestLapText;
    [SerializeField] private TMP_Text finishedStatsText;
    [SerializeField] private GameObject raceFinishedPanel;

    [Header("End Screens")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private SpeeederController playerController;
    [SerializeField] private float endScreenDelay = 1.5f;

    [Header("AI Tracking")]
    [SerializeField] private AIRacer aiRacer;

    private int   currentLap    = 1;
    private float raceTimer     = 0f;
    private float lapTimer      = 0f;
    private float bestLapTime   = Mathf.Infinity;
    private bool  raceStarted   = false;
    private bool  raceFinished  = false;
    private bool  firstCrossDone = false;

    private int   aiCurrentLap  = 0;
    private float aiLastProgress = 0f;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (raceFinishedPanel != null)
            raceFinishedPanel.SetActive(false);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        if (deathPanel != null)
            deathPanel.SetActive(false);

        UpdateLapText();
        UpdateTimerText(0f);
        UpdateBestLapText();
    }

    private void Update()
    {
        if (!raceStarted || raceFinished) return;

        raceTimer += Time.deltaTime;
        lapTimer  += Time.deltaTime;

        UpdateTimerText(lapTimer);
        UpdateBestLapText();

        // AI lap tracking
        if (aiRacer != null)
        {
            float aiProgress = aiRacer.GetProgress();
            if (aiLastProgress > 0.9f && aiProgress < 0.1f)
            {
                aiCurrentLap++;
                if (aiCurrentLap > totalLaps)
                {
                    StartCoroutine(ShowDefeat());
                }
            }
            aiLastProgress = aiProgress;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Appelé par FinishLineTrigger quand le joueur franchit la ligne.</summary>
    public void OnPlayerCrossedFinishLine()
    {
        if (raceFinished) return;

        if (!firstCrossDone)
        {
            // Premier passage sur la ligne — démarre la course, on est déjà au tour 1
            firstCrossDone = true;
            raceStarted    = true;
            raceTimer      = 0f;
            lapTimer       = 0f;
            UpdateLapText();
            return;
        }

        // Fin d'un tour
        if (lapTimer < bestLapTime)
            bestLapTime = lapTimer;

        currentLap++;
        lapTimer = 0f;

        // Toujours mettre à jour l'affichage, même sur le dernier tour
        UpdateLapText();

        if (currentLap > totalLaps)
        {
            StartCoroutine(ShowVictory());
            return;
        }

        FindFirstObjectByType<RaceDialogueTrigger>()?.OnLapCompleted(currentLap);
    }

    /// <summary>Appelé par VehicleHealth quand le joueur est détruit.</summary>
    public void PlayerDestroyed()
    {
        if (!raceFinished)
            StartCoroutine(ShowDefeat());
    }

    // -------------------------------------------------------------------------
    // End screens
    // -------------------------------------------------------------------------

    private IEnumerator ShowVictory()
    {
        raceFinished = true;

        if (raceFinishedPanel != null)
            raceFinishedPanel.SetActive(true);

        if (finishedStatsText != null)
        {
            finishedStatsText.text =
                $"Temps total  {FormatTime(raceTimer)}\n" +
                $"Meilleur tour  {FormatTime(bestLapTime)}";
        }

        yield return new WaitForSeconds(endScreenDelay);

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        Time.timeScale = 0f;

        if (raceFinishedPanel != null)
            raceFinishedPanel.SetActive(false);

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    private IEnumerator ShowDefeat()
    {
        raceFinished = true;

        yield return new WaitForSeconds(endScreenDelay);

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        Time.timeScale = 0f;

        if (deathPanel != null)
            deathPanel.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void UpdateLapText()
    {
        if (lapText == null) return;
        lapText.text = $"TOUR {currentLap} / {totalLaps}";
    }

    private void UpdateTimerText(float time)
    {
        if (timerText == null) return;
        timerText.text = FormatTime(time);
    }

    private void UpdateBestLapText()
    {
        if (bestLapText == null) return;
        bestLapText.text = bestLapTime < Mathf.Infinity
            ? $"BEST  {FormatTime(bestLapTime)}"
            : "BEST  --:--.---";
    }

    private static string FormatTime(float seconds)
    {
        if (seconds >= Mathf.Infinity) return "--:--.---";
        int   mins  = Mathf.FloorToInt(seconds / 60f);
        int   secs  = Mathf.FloorToInt(seconds % 60f);
        int   ms    = Mathf.FloorToInt((seconds % 1f) * 1000f);
        return $"{mins:00}:{secs:00}.{ms:000}";
    }

    // -------------------------------------------------------------------------
    // Boutons UI
    // -------------------------------------------------------------------------

    /// <summary>Redémarre la scène courante.</summary>
    public void RestartRace()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Charge la scène du menu principal.</summary>
    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
