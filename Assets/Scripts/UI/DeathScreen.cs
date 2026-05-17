using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the "DESTROYED" defeat screen panel.
/// Shown when the player's VehicleHealth reaches zero.
/// Attach to the DeathPanel GameObject (disabled by default).
/// </summary>
public class DeathScreen : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Activates the death panel.</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>Restarts the current scene (wired to Restart button).</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Returns to the main menu — scene index 0 (wired to Menu button).</summary>
    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
