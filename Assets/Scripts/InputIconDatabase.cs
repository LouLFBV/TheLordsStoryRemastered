using UnityEngine;
using System.Collections.Generic;

public class InputIconDatabase : MonoBehaviour
{
    public static InputIconDatabase instance;

    [Header("GAMEPAD ICON SETS")]
    public ButtonIconSet xboxSet;
    public ButtonIconSet playstationSet;
    public ButtonIconSet switchSet;

    [Header("KEYBOARD / MOUSE ICON SETS")]
    public ButtonIconSet keyboardSet;
    public ButtonIconSet mouseSet;

    private ButtonIconSet activeGamepadSet;

    private void Awake()
    {
        instance = this;
        UpdateGamepadSet();
    }

    // -------------------------------------------------------------
    // DÉTECTION AUTOMATIQUE DU GAMEPAD ACTIF
    // -------------------------------------------------------------
    public void UpdateGamepadSet()
    {
        var type = GamepadDetector.DetectCurrentGamepad();

        switch (type)
        {
            case GamepadType.PlayStation:
                activeGamepadSet = playstationSet;
                break;

            case GamepadType.Switch:
                activeGamepadSet = switchSet;
                break;

            default:
                activeGamepadSet = xboxSet;
                break;
        }
    }

    // -------------------------------------------------------------
    // OBTENIR L'ICÔNE (TOUS DEVICES)
    // -------------------------------------------------------------
    public Sprite GetIcon(string controlPath)
    {
        if (string.IsNullOrEmpty(controlPath))
            return null;

        // ------------------ GAMEPAD ------------------
        if (controlPath.Contains("Gamepad") || controlPath.Contains("DualShock") || controlPath.Contains("DualSense"))
        {
            if (activeGamepadSet == null)
                UpdateGamepadSet();

            string normalizedPath = controlPath;
            if (controlPath.Contains("DualShock") || controlPath.Contains("DualSense"))
            {
                normalizedPath = controlPath.Replace("DualShockGamepadHID", "<Gamepad>").Replace("DualSenseGamepadHID", "<Gamepad>");
            }

            return activeGamepadSet?.GetIcon(normalizedPath);
        }

        // ------------------ KEYBOARD ------------------
        if (controlPath.Contains("<Keyboard>"))
        {
            return keyboardSet?.GetIcon(controlPath);
        }

        // ------------------ MOUSE ---------------------
        if (controlPath.Contains("<Mouse>"))
        {
            return mouseSet?.GetIcon(controlPath);
        }

        // ------------------ UNKNOWN -------------------
        return null; // laisser texte en fallback
    }
}
