using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InputRebindUI : MonoBehaviour
{
    [Header("Références")]
    public PlayerInput playerInput;
    public TextMeshProUGUI bindingText;
    public Image iconField;

    [Header("Action & Binding")]
    public string actionName;
    public int bindingIndex;

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    private void Start()
    {
        RefreshDisplay();
    }

    public void StartRebind()
    {
        if (UIManagerSystem.Instance != null) UIManagerSystem.Instance.ToggleCursor(false);

        InputAction action = playerInput.actions[actionName];

        iconField.enabled = false;
        bindingText.text = "...";

        // Désactiver toutes les actions
        playerInput.actions.Disable();

        var binding = action.bindings[bindingIndex];
        string path = binding.effectivePath;

        rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
        .WithCancelingThrough("<Keyboard>/escape")
        .OnMatchWaitForAnother(0.1f);

        //  BLOQUAGE CROISÉ CLAVIER / MANETTE
        if (!string.IsNullOrEmpty(path))
        {
            // Si c'est un binding clavier / souris  on interdit la manette
            if (path.Contains("<Keyboard>") || path.Contains("<Mouse>"))
            {
                rebindOperation.WithControlsExcluding("<Gamepad>");
            }
            // Si c'est un binding manette  on interdit clavier + souris
            else if (path.Contains("<Gamepad>"))
            {
                rebindOperation
                    .WithControlsExcluding("<Keyboard>")
                    .WithControlsExcluding("<Mouse>");
            }
        }

        rebindOperation
        .OnComplete(operation =>
        {
            operation.Dispose();
            playerInput.actions.Enable();
            // 2. Réactive le curseur après la saisie
            if (UIManagerSystem.Instance != null) UIManagerSystem.Instance.ToggleCursor(true);
            FinishRebind();
        })
        .OnCancel(operation =>
        {
            operation.Dispose();
            playerInput.actions.Enable();
            // 2. Réactive le curseur après annulation
            if (UIManagerSystem.Instance != null) UIManagerSystem.Instance.ToggleCursor(true);
            RefreshDisplay();
        });

        rebindOperation.Start();

    }



    private void FinishRebind()
    {
        // Sauvegarder automatiquement
        InputRebindManager.SaveRebinds(playerInput);
        
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        InputAction action = playerInput.actions[actionName];
        InputBindingDisplay.UpdateDisplay(
        action,
        bindingIndex,
        iconField
    );
    }

}
