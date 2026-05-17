using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles main menu button actions : launching the game with a loading screen, and quitting.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string GameSceneName = "SampleScene";

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private float loadingDuration = 6f;

    /// <summary>Starts the loading sequence then loads the game scene.</summary>
    public void PlayGame()
    {
        StartCoroutine(LoadGameSequence());
    }

    /// <summary>Quits the application. In the Editor, stops Play mode.</summary>
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadGameSequence()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(true);

        yield return new WaitForSeconds(loadingDuration);

        SceneManager.LoadScene(GameSceneName);
    }
}
