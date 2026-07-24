using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Audio;

public class UIManagerSystem : MonoBehaviour
{
    public static UIManagerSystem Instance;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Menu menu;

    [SerializeField] private GameObject crosshair;

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject questsPanel;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject tooltipPanel;

    [SerializeField] private GameObject pauseMenuPanel;

    [Header("HUD Elements")]
    public List<GameObject> hudElements;

    [Header("Cursor Settings")]
    [SerializeField] private float cursorSpeed = 1000f;
    private bool _isCursorVisible = false;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 20f;
    [SerializeField] private float scrollSensitivity = 0.025f;


    [Header("Click Settings")]
    [SerializeField] private float clickCooldown = 0.2f; // Anti-rebond (200ms)
    private float lastClickTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ToggleCursor(false);
    }


    private void Update()
    {
        if (!_isCursorVisible || GamepadDetector.DetectCurrentGamepad() == GamepadType.None) return;

        Vector2 stickValue = PlayerController.Instance.Input.NavigateLook;

        if (stickValue.magnitude > 0.1f)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 newMousePos = currentMousePos + (stickValue * cursorSpeed * Time.unscaledDeltaTime);
            newMousePos.x = Mathf.Clamp(newMousePos.x, 0, Screen.width);
            newMousePos.y = Mathf.Clamp(newMousePos.y, 0, Screen.height);
            Mouse.current.WarpCursorPosition(newMousePos);
        }

        Vector2 scrollInput = PlayerController.Instance.Input.GamepadScroll;
        if (scrollInput.y != 0)
        {
            SimulateScroll(scrollInput.y * scrollSpeed);
        }

        if (PlayerController.Instance.Input.SubmitPressed)
        {
            if (Time.unscaledTime - lastClickTime >= clickCooldown)
            {
                lastClickTime = Time.unscaledTime;
                SimulateMouseClick();
            }
            PlayerController.Instance.Input.UseSubmitInput();
        }
    }

    private void SimulateMouseClick()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue(),
            button = PointerEventData.InputButton.Left
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            GameObject clickedObject = results[0].gameObject;
            GameObject targetHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(clickedObject);

            if (targetHandler != null)
            {
                Debug.Log($"[UIManager] Élément interactif ciblé : {targetHandler.name}");

                // Uniquement l'exécution du clic pointeur
                ExecuteEvents.Execute(targetHandler, eventData, ExecuteEvents.pointerClickHandler);

                // 🟢 On désélectionne l'objet pour empêcher EventSystem d'envoyer un OnSubmit natif en parallèle
                EventSystem.current.SetSelectedGameObject(null);

                var rebindComp = targetHandler.GetComponentInParent<InputRebindUI>();
                if (rebindComp != null)
                {
                    ExecuteEvents.Execute(rebindComp.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                    Debug.Log($"[UIManager] Clic rebind effectué sur {rebindComp.gameObject.name}");
                }
            }
        }
    }

    private void SimulateScroll(float scrollAmount)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            ScrollRect scrollRect = result.gameObject.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                float scrollDelta = scrollAmount * scrollSensitivity * Time.unscaledDeltaTime;
                float newPos = scrollRect.verticalNormalizedPosition + scrollDelta;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
                break;
            }
        }
    }

    public void ToggleCursor(bool isVisible)
    {
        _isCursorVisible = isVisible;
        Cursor.visible = isVisible;
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OpenPanel(UIPanelType type)
    {
        CloseAllPanelsVisuals();
        MuteSFX(); 

        switch (type)
        {
            case UIPanelType.Inventory:
                inventoryPanel.SetActive(true);
                break;
            case UIPanelType.PauseMenu:
                pauseMenuPanel.SetActive(true);
                break;
            // 🟢 Ajout des autres types pour harmoniser ton système
            case UIPanelType.Quests:
                OpenQuestsAndCloseOthers();
                break;
            case UIPanelType.Map:
                mapPanel.SetActive(true);
                break;
        }
    }

    public void CloseAll()
    {
        CloseAllPanelsVisuals();
        RestoreSFX(); 
    }

    private void CloseAllPanelsVisuals()
    {
        inventoryPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        questsPanel.SetActive(false);
        equipmentPanel.SetActive(false);
        mapPanel.SetActive(false);
        tooltipPanel.SetActive(false);

        if (menu != null) menu.CloseAllSettingsPanel();
        if (NewQuestLog.instance != null) NewQuestLog.instance.DesactivePanel();

        ActiveDesactiveHUD(true);
    }

    private void MuteSFX()
    {
        if (audioMixer != null)
        {
            // On coupe le son instantanément
            audioMixer.SetFloat("SFXVolume", -80f);
        }
    }

    private void RestoreSFX()
    {
        if (audioMixer != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

            // Sécurité : On empêche savedVolume de valoir 0 pour éviter le -Infinity
            if (savedVolume <= 0.001f) savedVolume = 0.0001f;

            float targetdB = Mathf.Log10(savedVolume) * 20;
            audioMixer.ClearFloat("SFXVolume"); // On réinitialise le canal pour effacer le snapshot de pause
            audioMixer.SetFloat("SFXVolume", targetdB); // On applique la vraie valeur immédiatement
        }
    }

    public void ShowCrosshair(bool show)
    {
        if (crosshair != null) crosshair.SetActive(show);
    }

    public void TriggerRecipeFade(GameObject canvas, CanvasGroup canvasGroup, string itemName, Sprite icon, float fadeDuration, float displayDuration)
    {
        StartCoroutine(GlobalFadeRoutine(canvas, canvasGroup, itemName, icon, fadeDuration, displayDuration));
    }

    private IEnumerator GlobalFadeRoutine(GameObject canvas, CanvasGroup canvasGroup, string itemName, Sprite icon, float fade, float display)
    {
        canvas.SetActive(true);
        canvasGroup.alpha = 0;

        float t = 0;
        while (t < fade)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fade;
            yield return null;
        }
        canvasGroup.alpha = 1;

        yield return new WaitForSeconds(display);

        t = 0;
        while (t < fade)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1 - (t / fade);
            yield return null;
        }
        canvasGroup.alpha = 0;
        canvas.SetActive(false);
    }

    #region --- Méthodes d'ouverture spécifiques (Boutons d'onglets) ---
    public void OpenInventoryAndCloseOthers()
    {
        CloseAllPanelsVisuals();
        inventoryPanel.SetActive(true);
    }
    public void OpenQuestsAndCloseOthers()
    {
        CloseAllPanelsVisuals();
        if (NewQuestLog.instance != null && NewQuestManager.instance != null)
        {
            NewQuestLog.instance.OnAffichageQuestPanel(NewQuestManager.instance.activeQuests);
        }
        questsPanel.SetActive(true);
    }
    public void OpenEquipmentAndCloseOthers()
    {
        CloseAllPanelsVisuals();
        ActiveDesactiveHUD(false);
        equipmentPanel.SetActive(true);
    }
    public void OpenMapAndCloseOthers()
    {
        CloseAllPanelsVisuals();
        mapPanel.SetActive(true);
    }

    private void ActiveDesactiveHUD(bool actived)
    {
        foreach (GameObject element in hudElements)
        {
            if (element != null)
                element.SetActive(actived);
        }
    }
    #endregion 
}