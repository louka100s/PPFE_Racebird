using UnityEngine;

/// <summary>
/// Trigger placé sur la ligne de départ/arrivée.
/// Notifie le LapManager quand le joueur le franchit.
/// Le cooldown empêche les doubles comptages si le véhicule reste sur la ligne.
/// </summary>
public class FinishLineTrigger : MonoBehaviour
{
    private const float Cooldown = 2f;

    private float lastTriggerTime = -10f;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastTriggerTime < Cooldown) return;

        if (other.GetComponentInParent<SpeeederController>() == null) return;

        lastTriggerTime = Time.time;

        if (LapManager.Instance != null)
            LapManager.Instance.OnPlayerCrossedFinishLine();
    }
}
