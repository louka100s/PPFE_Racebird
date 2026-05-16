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

    private int   currentLap    = 0;
    private float raceTimer     = 0f;
    private float lapTimer      = 0f;
    private float bestLapTime   = Mathf.Infinity;
    private bool  raceStarted   = false;
    private bool  raceFinished  = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (raceFinishedPanel != null)
            raceFinishedPanel.SetActive(false);

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
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Appelé par FinishLineTrigger quand le joueur franchit la ligne.</summary>
    public void OnPlayerCrossedFinishLine()
    {
        if (raceFinished) return;

        if (currentLap == 0)
        {
            // Premier passage — démarre la course
            raceStarted = true;
            currentLap  = 1;
            raceTimer   = 0f;
            lapTimer    = 0f;
        }
        else
        {
            // Fin d'un tour
            if (lapTimer < bestLapTime)
                bestLapTime = lapTimer;

            currentLap++;
            lapTimer = 0f;

            if (currentLap > totalLaps)
            {
                FinishRace();
                return;
            }

            FindFirstObjectByType<RaceDialogueTrigger>()?.OnLapCompleted(currentLap);
        }

        UpdateLapText();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void FinishRace()
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
    }

    private void UpdateLapText()
    {
        if (lapText == null) return;
        lapText.text = currentLap == 0
            ? $"LAP — / {totalLaps}"
            : $"LAP {currentLap} / {totalLaps}";
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

    /// <summary>Charge la scène 0 (menu principal).</summary>
    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
