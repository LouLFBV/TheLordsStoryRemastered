using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            case GamepadType.Xbox:
                activeGamepadSet = xboxSet;
                break;
            default:
                activeGamepadSet = playstationSet;
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

        // 1. Déterminer quel set utiliser
        ButtonIconSet targetSet = null;

        if (controlPath.Contains("Gamepad") || controlPath.Contains("DualShock") || controlPath.Contains("DualSense") || controlPath.Contains("Joystick"))
        {
            if (activeGamepadSet == null) UpdateGamepadSet();
            targetSet = activeGamepadSet;
        }
        else if (controlPath.Contains("<Keyboard>")) targetSet = keyboardSet;
        else if (controlPath.Contains("<Mouse>")) targetSet = mouseSet;

        if (targetSet == null) return null;

        // 2. Trouver l'icône en comparant la fin du chemin (ex: "select")
        // On extrait la partie après le dernier slash (ex: "select")
        string key = controlPath.Split('/').Last();

        // On cherche dans le set une entrée qui se termine par ce nom
        var entry = targetSet.icons.FirstOrDefault(i => i.controlPath.Split('/').Last() == key);

        return entry?.icon;
    }
}
