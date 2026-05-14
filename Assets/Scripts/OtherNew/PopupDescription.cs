using UnityEngine;
using TMPro;
using System.Collections;
public class PopupDescription : WorldDisappearOnCollected
{
    [SerializeField] private GameObject popupDescriptionPanel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private string description;


    [Header("UI Animation")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 5f;

    private bool _isUsed = false;
    protected override void OnEnable()
    {
        PopupEvent.OnPopupRequested += ShowDescriptionPanel;
    }

    protected override void OnDisable()
    {
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
                Debug.LogWarning($"<color=purple>[{name}] registered as collected in WorldStateManager, with ID : {worldID.UniqueID}.</color>");
            }
        }
    }
    private void ShowDescriptionPanel(string desc)
    {

        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Add(popupDescriptionPanel);
        popupDescriptionPanel.SetActive(true);
        descriptionText.text = desc;

        StopAllCoroutines();
        StartCoroutine(FadeDescriptionPanel());
    }
    private IEnumerator FadeDescriptionPanel()
    {
        popupCanvasGroup.alpha = 0;

        // --- FADE IN ---
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            popupCanvasGroup.alpha = t / fadeDuration;
            yield return null;
        }

        popupCanvasGroup.alpha = 1;

        // --- ATTENTE ---
        yield return new WaitForSeconds(displayDuration);

        // --- FADE OUT ---
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            popupCanvasGroup.alpha = 1 - (t / fadeDuration);
            yield return null;
        }

        popupCanvasGroup.alpha = 0;
        popupDescriptionPanel.SetActive(false);
    }
}
