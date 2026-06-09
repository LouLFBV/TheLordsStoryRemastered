using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Indispensable pour Mouse.current et Gamepad.current
using UnityEngine.EventSystems;

public class UIMainMenu : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private float cursorSpeed = 1300f;

    [Header("UI References")]
    [SerializeField] private GameObject creditUI; 

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        // On vérifie si une manette est branchée via le New Input System
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // 1. Déplacement avec le Stick Gauche
        Vector2 stickValue = gamepad.leftStick.ReadValue() + gamepad.dpad.ReadValue();

        if (stickValue.magnitude > 0.1f)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 newMousePos = currentMousePos + (stickValue * cursorSpeed * Time.unscaledDeltaTime);

            // Clamp pour ne pas sortir de l'écran
            newMousePos.x = Mathf.Clamp(newMousePos.x, 0, Screen.width);
            newMousePos.y = Mathf.Clamp(newMousePos.y, 0, Screen.height);

            // On déplace réellement le curseur Windows/OSX
            Mouse.current.WarpCursorPosition(newMousePos);
        }

        // 2. Clic avec la touche "Croix" (PlayStation) ou "A" (Xbox)
        // buttonSouth correspond au bouton du bas sur le pad droit
        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            SimulateMouseClick();
        }
    }

    private void SimulateMouseClick()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            GameObject clickedObject = results[0].gameObject;

            // On simule toute la séquence de clic pour être sûr que le bouton réagisse
            ExecuteEvents.Execute(clickedObject, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(clickedObject, eventData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(clickedObject, eventData, ExecuteEvents.pointerUpHandler);

            EventSystem.current.SetSelectedGameObject(clickedObject);
        }
    }

    public void ShowCredits()
    {
        if (creditUI != null)
        {
            creditUI.SetActive(!creditUI.activeSelf);
        }
    }
}