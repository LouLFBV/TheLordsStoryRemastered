using UnityEngine;
using TMPro;
using System.Collections;

public class PopupDescription : WorldDisappearOnCollected
{
    [Header("UI References")]
    [SerializeField] private GameObject popupDescriptionPanel;
    [SerializeField] private Transform horizontalLayoutGroup;

    [Header("Prefabs")]
    [SerializeField] private GameObject textChunkPrefab;
    [SerializeField] private GameObject iconChunkPrefab;

    [SerializeField, TextArea] private string description;

    [Header("UI Animation")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 5f;

    private bool _isUsed = false;

    protected override void OnEnable()
    {
        base.OnEnable(); 
        PopupEvent.OnPopupRequested += ShowDescriptionPanel;
    }

    protected override void OnDisable()
    {
        base.OnDisable(); 
        PopupEvent.OnPopupRequested -= ShowDescriptionPanel;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isUsed)
        {
            ShowDescriptionPanel(description);
            _isUsed = true;
            if (worldID != null)
            {
                WorldStateManager.Instance.RegisterCollectedObject(worldID.UniqueID);
            }
        }
    }

    private void ShowDescriptionPanel(string desc)
    {
        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Add(popupDescriptionPanel);
        if (PopupParent.Instance != null)
            popupDescriptionPanel.transform.SetParent(PopupParent.Instance.parentPopup, false);

        popupDescriptionPanel.SetActive(true);

        foreach (Transform child in horizontalLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        GenerateDynamicContent(desc);

        StopAllCoroutines();
        StartCoroutine(FadeDescriptionPanel());
    }

    private void GenerateDynamicContent(string rawText)
    {
        string[] parts = rawText.Split('{');

        if (!string.IsNullOrEmpty(parts[0]))
        {
            CreateTextChunk(parts[0]);
        }

        for (int i = 1; i < parts.Length; i++)
        {
            string[] subParts = parts[i].Split('}');

            if (subParts.Length > 0)
            {
                string actionName = subParts[0];

                CreateDualIconChunk(actionName);

                if (subParts.Length > 1 && !string.IsNullOrEmpty(subParts[1]))
                {
                    CreateTextChunk(subParts[1]);
                }
            }
        }
    }

    private void CreateTextChunk(string text)
    {
        GameObject textObj = Instantiate(textChunkPrefab, horizontalLayoutGroup);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
    }

    private void CreateDualIconChunk(string actionName)
    {
        // 1. Icône Clavier/Souris
        GameObject iconCSObj = Instantiate(iconChunkPrefab, horizontalLayoutGroup);
        IconeUI iconeCS = iconCSObj.GetComponent<IconeUI>();

        iconeCS.SetActionAndDevice(actionName, true, DeviceType.Keyboard);

        // 2. Le slash de séparation
        CreateTextChunk("/");

        // 3. Icône Manette
        GameObject iconGamepadObj = Instantiate(iconChunkPrefab, horizontalLayoutGroup);
        IconeUI iconeGamepad = iconGamepadObj.GetComponent<IconeUI>();

        iconeGamepad.SetActionAndDevice(actionName, true, DeviceType.Gamepad);
    }

    private IEnumerator FadeDescriptionPanel()
    {
        popupCanvasGroup.alpha = 0;

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            popupCanvasGroup.alpha = t / fadeDuration;
            yield return null;
        }

        popupCanvasGroup.alpha = 1;
        yield return new WaitForSeconds(displayDuration);

        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            popupCanvasGroup.alpha = 1 - (t / fadeDuration);
            yield return null;
        }

        popupCanvasGroup.alpha = 0;
        popupDescriptionPanel.SetActive(false);
        Destroy(popupDescriptionPanel);
    }
}