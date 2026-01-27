using UnityEngine;
using UnityEngine.InputSystem;

public class InputDiagnostic : MonoBehaviour
{
    private PlayerInput playerInput;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput == null)
        {
            Debug.LogError("[InputDiagnostic] Aucun composant PlayerInput trouvé !");
            return;
        }

        Debug.Log($"[InputDiagnostic] PlayerInput configuré:");
        Debug.Log($"  - Actions: {(playerInput.actions != null ? playerInput.actions.name : "NULL")}");
        Debug.Log($"  - Default Map: {playerInput.defaultActionMap}");
        Debug.Log($"  - Behavior: {playerInput.notificationBehavior}");
        Debug.Log($"  - Actif: {playerInput.inputIsActive}");
        
        if (playerInput.actions != null)
        {
            var moveAction = playerInput.actions.FindAction("Move");
            if (moveAction != null)
            {
                Debug.Log($"  - Action 'Move' trouvée, bindings:");
                foreach (var binding in moveAction.bindings)
                {
                    Debug.Log($"    * {binding.name}: {binding.effectivePath}");
                }
            }
            else
            {
                Debug.LogWarning("[InputDiagnostic] Action 'Move' introuvable !");
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.zKey.wasPressedThisFrame)
            Debug.Log("[InputDiagnostic] Touche Z détectée (brut)");
        if (Keyboard.current.sKey.wasPressedThisFrame)
            Debug.Log("[InputDiagnostic] Touche S détectée (brut)");
        if (Keyboard.current.qKey.wasPressedThisFrame)
            Debug.Log("[InputDiagnostic] Touche Q détectée (brut)");
        if (Keyboard.current.dKey.wasPressedThisFrame)
            Debug.Log("[InputDiagnostic] Touche D détectée (brut)");
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        Debug.Log($"[InputDiagnostic] OnMove reçu: X={input.x:F2}, Y={input.y:F2}");
    }
}
