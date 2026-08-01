using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Chest : InteractableBase
{
    #region Champs

    [Header("Description Panel")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image objectImage;

    [Header("Chest Parts")]
    [SerializeField] private GameObject topChest;
    [SerializeField] private Rigidbody topLock;
    [SerializeField] private Rigidbody bottomLock;

    [Header("Chest Settings")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private Vector3 openEulerAngles = new Vector3(0, 0, 90);
    [SerializeField] private bool isLocked = false;
    [SerializeField] private ItemData keyItem;

    [Header("Audio")]
    [SerializeField] private AudioSource openSound;
    [SerializeField] private AudioSource lockedSound;
    [SerializeField] private AudioSource unlockSound;

    [Header("Reward")]
    [SerializeField] private ItemData rewardItem;
    [SerializeField] private int rewardAmount = 1;

    [Header("Gold Reward")]
    [SerializeField] private int goldAmount = 0;
    [SerializeField] private GameObject goldVisual;

    [Header("UI Animation")]
    [SerializeField] private CanvasGroup descriptionCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float displayDuration = 5f;

    [Header("Popup Event")]
    [SerializeField] private bool triggerPopupOnOpen = false;
    [SerializeField] private string popupMessage;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen = false;
    private bool isAnimating = false;

    #endregion 
    // -------------------------------------------------------
    // INITIALISATION
    // -------------------------------------------------------

    private void Start()
    {

        // --- Chest Setup ---
        closedRotation = topChest.transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(openEulerAngles);


        Debug.Log("<color=cyan>[CHEST] Initialisation du coffre…</color>");

        if (isLocked)
            objectType = InteractableObjectType.Key;
        else
            objectType = InteractableObjectType.Chest;
        interactUI.SetInteractable(this);
    }




    private void OnEnable()
    {
        if (WorldStateManager.Instance != null)
        {
            //  1. On s'abonne avec Subscribe() au lieu du +=
            WorldStateManager.Instance.Subscribe(Apply);

            //  2. FIX : On vérifie IMMÉDIATEMENT si le coffre a déjà été ouvert
            Apply();
        }
    }

    private void OnDisable()
    {
        if (WorldStateManager.Instance != null)
        {
            //  On utilise Unsubscribe() au lieu du -=
            WorldStateManager.Instance.Unsubscribe(Apply);
        }
    }

    public void Apply()
    {
        if (TryGetComponent<WorldObjectID>(out var worldID))
        {
            if (WorldStateManager.Instance.IsCollected(worldID.UniqueID))
            {
                Debug.LogWarning($"<color=orange>[CHEST] Le coffre {name} ({worldID.UniqueID}) a déjà été ouvert. Destruction !</color>");
                Destroy(gameObject);
            }
        }
    }



    // -------------------------------------------------------
    // INTERACTION
    // -------------------------------------------------------

    public override void OnInteract(PlayerInteractor player)
    {
        if (isAnimating || isOpen) return;
        TryOpenChest();
    }


    private void TryOpenChest()
    {
        if (isAnimating) return;

        if (isLocked && InventorySystem.instance.KeyIsInInventory(keyItem))
            TryToOpenWithKey(keyItem);
        else if (!isOpen && !isLocked)
            StartCoroutine(OpenChest());
        else if (isLocked)
            lockedSound.PlayOneShot(lockedSound.clip);
    }

    public void TryToOpenWithKey(ItemData key)
    {
        if (!isLocked)
        {
            StartCoroutine(OpenChest());
            return;
        }
        // Cas 1 : la bonne clé est utilisée
        else if (key == keyItem)
        {
            InventorySystem.instance.RemoveItem(key);
            StartCoroutine(OpenChest());
            return;
        }
        else
        {
            lockedSound.Play();
        }
    }


    // -------------------------------------------------------
    // OPEN ANIMATION
    // -------------------------------------------------------

    private IEnumerator OpenChest()
    {
        isLocked = false;
        isAnimating = true;
        isOpen = true;

        interactUI.Hide();
        gameObject.tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Default");
        openSound.Play();

        // Reward immédiat
        if (rewardItem == null && goldAmount > 0)
        {
            goldVisual.GetComponent<Coin>().goldAmount = goldAmount;
            goldVisual.SetActive(true);
        }
        else if (rewardItem != null)
        {
            ShowDescriptionPanel();
        }

        if(triggerPopupOnOpen)
        {
            PopupEvent.Raise(popupMessage);
        }

        // Unlocking physics
        topLock.isKinematic = false;
        bottomLock.isKinematic = false;

        // Animation
        while (Quaternion.Angle(topChest.transform.rotation, openRotation) > 0.1f)
        {
            topChest.transform.rotation = Quaternion.Slerp(
                topChest.transform.rotation,
                openRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }

        topChest.transform.rotation = openRotation;

        if (TryGetComponent<WorldObjectID>(out var worldID))
        {
            WorldStateManager.Instance.RegisterCollectedObject(worldID.UniqueID);
        }
    }



    // -------------------------------------------------------
    // REWARD UI
    // -------------------------------------------------------

    private void ShowDescriptionPanel()
    {
        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Add(descriptionPanel);
        if (PopupParent.Instance != null)
            descriptionPanel.transform.SetParent(PopupParent.Instance.parentItem, false);
        descriptionPanel.SetActive(true);

        nameText.text = rewardItem.itemName;
        objectImage.sprite = rewardItem.visual;

        if (rewardAmount > 1)
        {
            amountText.text = $"+{rewardAmount}";
            for (int i = 0; i < rewardAmount; i++)
                InventorySystem.instance.AddItem(rewardItem);
        }
        else
        {
            amountText.text = "+1";
            InventorySystem.instance.AddItem(rewardItem);
        }

        //StopAllCoroutines(); // évite les overlaps
        StartCoroutine(FadeDescriptionPanel());
    }
    private IEnumerator FadeDescriptionPanel()
    {
        descriptionCanvasGroup.alpha = 0;

        // --- FADE IN ---
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            descriptionCanvasGroup.alpha = t / fadeDuration;
            yield return null;
        }

        descriptionCanvasGroup.alpha = 1;

        // --- ATTENTE ---
        yield return new WaitForSeconds(displayDuration);

        // --- FADE OUT ---
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            descriptionCanvasGroup.alpha = 1 - (t / fadeDuration);
            yield return null;
        }

        descriptionCanvasGroup.alpha = 0;
        descriptionPanel.SetActive(false);
        if (UIManagerSystem.Instance != null)
            UIManagerSystem.Instance.hudElements.Remove(descriptionPanel);
    }
}
