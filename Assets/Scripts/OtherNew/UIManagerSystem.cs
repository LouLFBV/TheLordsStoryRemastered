using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManagerSystem : MonoBehaviour
{
    public static UIManagerSystem Instance;

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ToggleCursor(false); // On commence sans le curseur
    }
    private void Update()
    {
        if (!_isCursorVisible || GamepadDetector.DetectCurrentGamepad() == GamepadType.None) return;
        Vector2 stickValue = PlayerController.Instance.Input.NavigateLook;

        // 1. Déplacement (On garde ton code, il est parfait)
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


        // 2. Clic (Plus permissif)
        if (PlayerController.Instance.Input.SubmitPressed)
        {
            // On simule le clic systématiquement si le curseur est affiché
            // (Sauf si tu as un système de navigation par flèches en parallèle 
            // qui tourne sur un autre script, mais même là, cliquer "là où est la souris" est plus safe)
            SimulateMouseClick();
            Debug.Log("Click");

            PlayerController.Instance.Input.UseSubmitInput();
        }
    }

    private void SimulateMouseClick()
    {
        // 1. Créer une donnée d'événement de pointeur
        PointerEventData eventData = new PointerEventData(EventSystem.current);

        // 2. Lui donner la position actuelle de la souris
        eventData.position = Mouse.current.position.ReadValue();

        // 3. Faire un Raycast sur l'UI pour voir ce qu'il y a sous la souris
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            // On prend le premier objet touché (le plus en avant)
            GameObject clickedObject = results[0].gameObject;

            // 4. Simuler le clic (PointerDown + PointerUp = Click)
            ExecuteEvents.Execute(clickedObject, eventData, ExecuteEvents.pointerClickHandler);

            // Optionnel : Forcer le focus de l'EventSystem sur cet objet
            EventSystem.current.SetSelectedGameObject(clickedObject);
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
            // On cherche un composant ScrollRect dans l'objet touché ou ses parents
            ScrollRect scrollRect = result.gameObject.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                // On applique le scroll
                float scrollDelta = scrollAmount * scrollSensitivity * Time.unscaledDeltaTime;

                float newPos = scrollRect.verticalNormalizedPosition + scrollDelta;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
                break; // On ne scroll que le premier panneau trouvé
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
        // Ferme tout d'abord
        CloseAll();

        switch (type)
        {
            case UIPanelType.Inventory:
                inventoryPanel.SetActive(true);
                break;
            case UIPanelType.PauseMenu:
                pauseMenuPanel.SetActive(true);
                break;
        }
    }

    public void CloseAll()
    {
        inventoryPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        questsPanel.SetActive(false);
        equipmentPanel.SetActive(false);
        mapPanel.SetActive(false);
        tooltipPanel.SetActive(false);
        menu.CloseAllSettingsPanel();
        NewQuestLog.instance.DesactivePanel();
        ActiveDesactiveHUD(true);
    }


    public void ShowCrosshair(bool show)
    {
        if (crosshair != null)
        {
            crosshair.SetActive(show);
        }
    }

    #region --- Méthodes d'ouverture spécifiques pour les boutons de l'UI ---
    public void OpenInventoryAndCloseOthers()
    {
        CloseAll();
        inventoryPanel.SetActive(true);
    }
    public void OpenQuestsAndCloseOthers()
    {
        CloseAll();
        NewQuestLog.instance.OnAffichageQuestPanel(NewQuestManager.instance.activeQuests);
        questsPanel.SetActive(true);
    }
    public void OpenEquipmentAndCloseOthers()
    {
        CloseAll();
        ActiveDesactiveHUD(false);
        equipmentPanel.SetActive(true);
    }
    public void OpenMapAndCloseOthers()
    {
        CloseAll();
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