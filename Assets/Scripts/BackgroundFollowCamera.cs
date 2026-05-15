using UnityEngine;

/// <summary>
/// Keeps the background dome centered on the main camera at all times,
/// so it always fills the horizon regardless of where the player is.
/// </summary>
public class BackgroundFollowCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 pos = targetCamera.transform.position;
        pos.y = transform.position.y;
        transform.position = pos;
    }
}
