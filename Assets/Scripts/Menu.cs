using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class Menu : MonoBehaviour
{
    public Button newGameButton;
    public Button loadGameButton;
    public Button loadLastGameButton;
    public Button clearSavedDataButton;
    [SerializeField] private Animator animatorPanelChargerPartie;

    [SerializeField]
    private Dropdown resolutionDropdown;

    [SerializeField]
    private Dropdown qualitiesDropdown;

    [SerializeField]
    private Slider volumeSlider;

    [SerializeField]
    private Toggle fullScreenToggle;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject controllerInputPanel;
    [SerializeField] private GameObject keyboardInpuPanel;

    [SerializeField] private bool isMainMenu = false;
    [SerializeField] private GameObject partiesPanel;

    [Header("Input Settings UI")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Slider gamepadSensitivitySlider;
    [SerializeField] private Slider deadzoneSlider;

    [Header("UI for Menu")]
    [SerializeField] private Image iconeInputLoadGame;
    [SerializeField] private Image iconeInputDeleteGame;
    [SerializeField] private Sprite iconeLoadGameKeyboard;
    [SerializeField] private Sprite iconeDeleteGameKeyboard;
    [SerializeField] private Sprite iconeLoadGameGamepad;
    [SerializeField] private Sprite iconeDeleteGameGamepad;


    static private int pendingSlot;
    private bool isNewGame;
    private bool isTransitioning = false;
    private static bool _backToMenu = false;
    private static bool _saveInMenu = false;


    public static Menu Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (isMainMenu)
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            loadLastGameButton.interactable = SaveManager.Instance.HasSave(pendingSlot);
            DisableSlotUI(false);

            ActiveNewGame();

            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            return;
        }



        


        // Initialisation des qualités graphiques
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);

        string[] qualities = QualitySettings.names;
        qualitiesDropdown.ClearOptions();

        List<string> qualityOptions = new List<string>();
        for (int i = 0; i < qualities.Length; i++)
        {
            qualityOptions.Add(qualities[i]);
        }

        qualitiesDropdown.AddOptions(qualityOptions);

        // Synchronisation du dropdown avec la qualité réelle
        qualitiesDropdown.value = QualitySettings.GetQualityLevel();
        qualitiesDropdown.RefreshShownValue();


        // Initialisation des différentes résolutions
        Resolution[] resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> resolutionsOptions = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " (" + resolutions[i].refreshRateRatio + " Hz) ";
            resolutionsOptions.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
        {
            var input = PlayerController.Instance.Input;
            mouseSensitivitySlider.value = input.mouseSensitivity;
            gamepadSensitivitySlider.value = input.gamepadSensitivity;
            deadzoneSlider.value = input.stickDeadzone;
        }

        resolutionDropdown.AddOptions(resolutionsOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Initialisation du mode plein écran
        fullScreenToggle.isOn = Screen.fullScreen;

        Time.timeScale = 1f; // Assurez-vous que le temps est normalisé au démarrage du menu

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            // Quitter
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Quit();
            }

            // Gestion des slots
            if (partiesPanel.activeInHierarchy && clearSavedDataButton.interactable && loadGameButton.interactable)
            {
                HandleSaveInput();
            }
        }
    }

    private void HandleSaveInput()
    {
        UpdateIcone();

        // 1. Détection Manette (Gamepad)
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonWest.wasPressedThisFrame) // Carré / X
            {
                LoadGame(pendingSlot);
                return;
            }
            if (Gamepad.current.buttonEast.wasPressedThisFrame) // Rond / B
            {
                OnDeleteSlot();
                return;
            }
        }

        // 2. Détection Clavier (Keyboard)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                LoadGame(pendingSlot);
            }
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                OnDeleteSlot();
            }
        }
    }

    private void UpdateIcone()
    {
        // On vérifie si une manette est connectée et utilisée
        bool isGamepad = Gamepad.current != null;

        iconeInputLoadGame.sprite = isGamepad ? iconeLoadGameGamepad : iconeLoadGameKeyboard;
        iconeInputDeleteGame.sprite = isGamepad ? iconeDeleteGameGamepad : iconeDeleteGameKeyboard;
    }

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Bootstrap")
        {
            Debug.Log("Menu Start: LoadScene Bootstrap");
            if (TransitionPanel.Instance != null)
                TransitionPanel.Instance.PlayTransitionIn();
        }
    }
    public void LoadScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
    public void LoadMenu()
    {
        _backToMenu = true;
        if (TransitionPanel.Instance != null)
        {
            Debug.Log("Menu: PlayTransitionOut");
            TransitionPanel.Instance.PlayTransitionOut();
        }
        else
        {
            Debug.LogWarning("Menu: TransitionPanel.Instance is null, loading MainMenu directly");
            SceneManager.LoadScene("MainMenu");
        }
    }
    public void Quit()
    {
        Application.Quit();
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolutions = Screen.resolutions[resolutionIndex];
        Screen.SetResolution(resolutions.width, resolutions.height, Screen.fullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }




    #region Save System


    public void NewGame()
    {
        int slot = -1;
        SaveManager saveManager = SaveManager.Instance;
        for (int i = 1; i <= 3; i++)
        {
            if (!saveManager.HasSave(i))
            {
                slot = i;
                break;
            }
        }
        if (isTransitioning) return;
        isTransitioning = true;

        pendingSlot = slot;
        isNewGame = true;
        TransitionPanel.Instance.PlayTransitionOut();
    }
    public void Continue()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        isNewGame = false;
        TransitionPanel.Instance.PlayTransitionOut();
    }
    public void SaveGame() 
    {
        _saveInMenu = true;
        if (TransitionPanel.Instance == null)
        {
            Debug.LogWarning("Menu: TransitionPanel.Instance is null, cannot play transition out");
            SaveManager.Instance.SaveGame();
            return;
        }
        TransitionPanel.Instance.PlayTransitionOut();
        SaveManager.Instance.SaveGame();
        TransitionPanel.Instance.Continue();

    }
    public void LoadGame(int slot)
    {
        pendingSlot = slot;
        isNewGame = false;
        TransitionPanel.Instance.PlayTransitionOut();
    }
    public void OpenPanelParties()
    {
        partiesPanel.SetActive(!partiesPanel.activeSelf);
        DisableSlotUI(false);
    }

    //  Appelé par l'Animation Event
    public void OnOpenAnimationFinished()
    {
        if (_saveInMenu)
        {
            _saveInMenu = false;
            return;
        }
        Debug.Log($"SceneManager.GetActiveScene().name : {SceneManager.GetActiveScene().name}, _backToMenu : {_backToMenu} ");
        if (SceneManager.GetActiveScene().name != "MainMenu" && _backToMenu)
        {
            Debug.Log("Menu: Transition in finished, loading MainMenu");
            SceneManager.LoadScene("MainMenu");
        }
        else if (isNewGame)
        {
            Debug.Log($"Menu: Transition in finished, starting new game in slot {pendingSlot}");
            SaveManager.Instance.SetCurrentSlot(pendingSlot);
            SceneManager.LoadScene("Donjon");
        }
        else
        {
            Debug.Log($"Menu: Transition in finished, loading game from slot {pendingSlot}");
            SaveManager.Instance.LoadGame(pendingSlot);
        }
        _backToMenu = false;
    }

    private void ActiveNewGame()
    {
        SaveManager saveManager = SaveManager.Instance;
        newGameButton.interactable = !(saveManager.HasSave(1) && saveManager.HasSave(2) && saveManager.HasSave(3));
    }


    public void SelectSlot(int slot)
    {
        pendingSlot = slot;
        DisableSlotUI(SaveManager.Instance.HasSave(slot));
    }

    private void DisableSlotUI(bool actived)
    {
        clearSavedDataButton.interactable = actived;
        loadGameButton.interactable = actived;
    }

    public void OnDeleteSlot()
    {
        SaveManager.Instance.DeleteSave(pendingSlot);
        ActiveNewGame();
        DisableSlotUI(false);
        loadLastGameButton.interactable = SaveManager.Instance.HasSave(pendingSlot);
    }


    public void ConfirmDeleteSlot(int slot)
    {
        // ouvrir un popup "Êtes-vous sûr ?"
    }


    #endregion

    #region Settings Panel

    public void OpenSettingsPanel()
    {
        settingsPanel.SetActive(true);
        optionsPanel.SetActive(true);
        controllerInputPanel.SetActive(false);
        keyboardInpuPanel.SetActive(false);
    }

    public void OpenControllerInputPanel()
    {
        controllerInputPanel.SetActive(true);
        keyboardInpuPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    public void OpenKeyboardInputPanel()
    {
        controllerInputPanel.SetActive(false);
        keyboardInpuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    public void OpenOptionsPanel()
    {
        optionsPanel.SetActive(true);
        controllerInputPanel.SetActive(false);
        keyboardInpuPanel.SetActive(false);
    }
    public void CloseAllSettingsPanel()
    {
        settingsPanel.SetActive(false);
        controllerInputPanel.SetActive(false);
        keyboardInpuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        PlayerPrefs.Save();
    }

    public void OnMouseSensitivityChanged(float value)
    {
        if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
        {
            PlayerController.Instance.Input.mouseSensitivity = value;
            // Optionnel : Sauvegarder immédiatement
            PlayerPrefs.SetFloat("MouseSensi", value);
        }
    }

    public void OnGamepadSensitivityChanged(float value)
    {
        if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
        {
            PlayerController.Instance.Input.gamepadSensitivity = value;
            PlayerPrefs.SetFloat("GamepadSensi", value);
        }
    }

    public void OnDeadzoneChanged(float value)
    {
        if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
        {
            PlayerController.Instance.Input.stickDeadzone = value;
            PlayerPrefs.SetFloat("Deadzone", value);
        }
    }

    #endregion
}
