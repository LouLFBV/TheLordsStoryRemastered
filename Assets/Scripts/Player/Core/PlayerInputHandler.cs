using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerInputHandler : MonoBehaviour
{

    [Header("Mouse Settings")]
    public float mouseSensitivity = 1f;

    [Header("Gamepad Settings")]
    public float gamepadSensitivity = 5f;
    public float gamepadAimSensitivity = 2.5f;
    public float stickDeadzone = 0.15f;

    public Vector2 MoveInput { get; private set; }
    public Vector2 MouseLook { get; private set; }

    private Vector2 rawGamepadLook;
    public Vector2 GamepadLook => rawGamepadLook * (AimHeld ? gamepadAimSensitivity : gamepadSensitivity);
    public Vector2 NavigateLook { get; private set; }
    public Vector2 GamepadScroll { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool AttackSpecialPressed { get; private set; }
    public bool RollPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool AimHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool Weapon1Pressed { get; private set; }
    public bool Weapon2Pressed { get; private set; }
    public bool Object1Pressed { get; private set; }
    public bool Object2Pressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public bool MenuPressed { get; private set; }
    public Vector2 NavigationInput { get; private set; }
    public bool SubmitPressed { get; private set; }
    public bool CancelPressed { get; private set; }
    public bool CloseMenuPressed { get; private set; }
    public bool CloseInventoryPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool DialogueNextPressed { get; private set; }
    public bool UseActionPressed { get; private set; }
    public bool EquipActionPressed { get; private set; }
    public bool DropActionPressed { get; private set; }
    public bool DestroyActionPressed { get; private set; }
    public bool UnequipActionPressed { get; private set; }
    public bool LockOnPressed { get; private set; }

    private PlayerInput input;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        LoadSensitivitySettings();
    }

    public void LoadSensitivitySettings()
    {
        if (PlayerPrefs.HasKey("MouseSensi"))
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensi");

        if (PlayerPrefs.HasKey("GamepadSensi"))
            gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSensi");

        if (PlayerPrefs.HasKey("GamepadAimSensi"))
            gamepadAimSensitivity = PlayerPrefs.GetFloat("GamepadAimSensi");

        if (PlayerPrefs.HasKey("Deadzone"))
            stickDeadzone = PlayerPrefs.GetFloat("Deadzone");
    }
    private void OnEnable()
    {
        // --- MOVE avec Deadzone ---
        input.actions["Move"].performed += ctx => {
            Vector2 raw = ctx.ReadValue<Vector2>();
            // Si le stick est moins incliné que la deadzone, on met à 0
            MoveInput = (raw.magnitude < stickDeadzone) ? Vector2.zero : raw;
        };
        input.actions["Move"].canceled += _ => MoveInput = Vector2.zero;

        // --- LOOK (Souris et Gamepad) ---
        // On multiplie par la sensibilité EN DIRECT pour que les changements dans l'UI soient immédiats
        input.actions["LookMouse"].performed += ctx => MouseLook = ctx.ReadValue<Vector2>() * mouseSensitivity;
        input.actions["LookMouse"].canceled += _ => MouseLook = Vector2.zero;

        input.actions["LookGamepad"].performed += ctx => rawGamepadLook = ctx.ReadValue<Vector2>();
        input.actions["LookGamepad"].canceled += _ => rawGamepadLook = Vector2.zero;

        // --- NAVIGATE (UI) ---
        input.actions["Navigate"].performed += ctx => {
            Vector2 raw = ctx.ReadValue<Vector2>();
            // Deadzone appliquée
            NavigateLook = (raw.magnitude < stickDeadzone) ? Vector2.zero : raw;
            NavigationInput = NavigateLook;
        };
        input.actions["Navigate"].canceled += _ => {
            NavigateLook = Vector2.zero;
            NavigationInput = Vector2.zero;
        };

        // --- SCROLL (UI) ---
        input.actions["Scroll"].performed += ctx => {
            Vector2 raw = ctx.ReadValue<Vector2>();
            // On utilise GamepadScroll UNIQUEMENT pour le défilement
            GamepadScroll = (raw.magnitude < stickDeadzone) ? Vector2.zero : raw;
        };
        input.actions["Scroll"].canceled += _ => {
            GamepadScroll = Vector2.zero;
        };



        input.actions["Attack"].performed += ctx => AttackPressed = true;
        input.actions["Attack"].canceled += ctx => AttackPressed = false;

        input.actions["AttackSpecial"].performed += ctx => AttackSpecialPressed = true;
        input.actions["AttackSpecial"].canceled += ctx => AttackSpecialPressed = false;

        input.actions["ForwardRoll"].performed += ctx => RollPressed = true;
        input.actions["ForwardRoll"].canceled += ctx => RollPressed = false;

        input.actions["Sprint"].performed += ctx => SprintHeld = true;
        input.actions["Sprint"].canceled += ctx => SprintHeld = false;

        input.actions["Crouch"].performed += ctx => CrouchPressed = true;
        input.actions["Crouch"].canceled += ctx => CrouchPressed = false;

        input.actions["Jump"].performed += ctx => JumpPressed = true;
        input.actions["Jump"].canceled += ctx => JumpPressed = false;

        input.actions["Inventory"].performed += ctx => InventoryPressed = true;
        input.actions["Inventory"].canceled += ctx => InventoryPressed = false;

        input.actions["Menu"].performed += ctx => MenuPressed = true;
        input.actions["Menu"].canceled += ctx => MenuPressed = false;

        input.actions["Interact"].performed += ctx => InteractPressed = true;
        input.actions["Interact"].canceled += ctx => InteractPressed = false;

        input.actions["LockOn"].performed += ctx => LockOnPressed = true;
        input.actions["LockOn"].canceled += ctx => LockOnPressed = false;


        input.actions["Aim"].performed += ctx => AimHeld = true;
        input.actions["Aim"].canceled += ctx => AimHeld = false;

        //Palette
        input.actions["Weapon1"].performed += ctx => Weapon1Pressed = true;
        input.actions["Weapon1"].canceled += ctx => Weapon1Pressed = false;

        input.actions["Weapon2"].performed += ctx => Weapon2Pressed = true;
        input.actions["Weapon2"].canceled += ctx => Weapon2Pressed = false;

        input.actions["Object1"].performed += ctx => Object1Pressed = true;
        input.actions["Object1"].canceled += ctx => Object1Pressed = false;

        input.actions["Object2"].performed += ctx => Object2Pressed = true;
        input.actions["Object2"].canceled += ctx => Object2Pressed = false;

        // UI

        //input.actions["Navigate"].performed += ctx => NavigationInput = ctx.ReadValue<Vector2>(); ;
        //input.actions["Navigate"].canceled += ctx => NavigationInput = Vector2.zero;

        input.actions["Submit"].performed += ctx => SubmitPressed = true;
        input.actions["Submit"].canceled += ctx => SubmitPressed = false;

        input.actions["Cancel"].performed += ctx => CancelPressed = true;
        input.actions["Cancel"].canceled += ctx => CancelPressed = false;

        input.actions["CloseMenu"].performed += ctx => CloseMenuPressed = true;
        input.actions["CloseMenu"].canceled += ctx => CloseMenuPressed = false;

        input.actions["CloseInventory"].performed += ctx => CloseInventoryPressed = true;
        input.actions["CloseInventory"].canceled += ctx => CloseInventoryPressed = false;


        input.actions["DialogueNext"].performed += ctx => DialogueNextPressed = true;
        input.actions["DialogueNext"].canceled += ctx => DialogueNextPressed = false;

        // Slot Actions 

        input.actions["UseAction"].performed += ctx => UseActionPressed = true;
        input.actions["UseAction"].canceled += ctx => UseActionPressed = false;

        input.actions["EquipAction"].performed += ctx => EquipActionPressed = true;
        input.actions["EquipAction"].canceled += ctx => EquipActionPressed = false;

        input.actions["DropAction"].performed += ctx => DropActionPressed = true;
        input.actions["DropAction"].canceled += ctx => DropActionPressed = false;

        input.actions["DestroyAction"].performed += ctx => DestroyActionPressed = true;
        input.actions["DestroyAction"].canceled += ctx => DestroyActionPressed = false;

        input.actions["UnequipAction"].performed += ctx => UnequipActionPressed = true;
        input.actions["UnequipAction"].canceled += ctx => UnequipActionPressed = false;
    }

    // --- LA MÉTHODE PRO ---
    public void SwitchActionMap(string mapName)
    {
        Debug.Log($"Switching to action map: {mapName}");
        input.SwitchCurrentActionMap(mapName);

        // Reset des valeurs pour éviter que le perso continue de courir 
        // si on ouvre l'inventaire en plein sprint
        MoveInput = Vector2.zero;
        GamepadScroll = Vector2.zero;
        AttackPressed = false;
        AttackSpecialPressed = false;
        RollPressed = false;
        SprintHeld = false;
        CrouchPressed = false;
        AimHeld = false;
        JumpPressed = false;
        Weapon1Pressed = false;
        Weapon2Pressed = false;
        Object1Pressed = false;
        Object2Pressed = false;
    }
    public void UseInventoryInput() => InventoryPressed = false;
    public void UseMenuInput() => MenuPressed = false;
    public void UseCloseMenuInput() => CloseMenuPressed = false;
    public void UseCloseInventoryInput() => CloseInventoryPressed = false;
    public void UseWeapon1Pressed() => Weapon1Pressed = false;
    public void UseWeapon2Pressed() => Weapon2Pressed = false;
    public void UseObject1Pressed() => Object1Pressed = false;
    public void UseObject2Pressed() => Object2Pressed = false;
    public void UseInteractInput() => InteractPressed = false;
    public void UseDialogueNextInput() => DialogueNextPressed = false;
    public void UseCrouchInput() => CrouchPressed = false;
    public void UseJumpInput() => JumpPressed = false;
    public void UseRollInput() => RollPressed = false;
    public void UseAttackInput() => AttackPressed = false;
    public void UseAttackSpecialInput() => AttackSpecialPressed = false;
    public void UseLockOnInput() => LockOnPressed = false;
    public void UseSubmitInput() => SubmitPressed = false;
    public void UseCancelInput() => CancelPressed = false;
    public void UseDropActionInput() => DropActionPressed = false;
    public void UseEquipActionInput() => EquipActionPressed = false;
    public void UseUseActionInput() => UseActionPressed = false;
    public void UseUnequipAction() => UnequipActionPressed = false;
}