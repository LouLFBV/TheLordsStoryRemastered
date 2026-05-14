using System.Collections;
using UnityEngine;

public class PersonalChest : InteractableBase
{

    [Header("Chest Parts")]
    [SerializeField] private GameObject topChest;

    [Header("Chest Settings")]
    [SerializeField] private ChestInventory chestInventory;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private Vector3 openEulerAngles = new Vector3(0, 0, 90);

    [SerializeField] private AudioSource Opensound;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    public bool isOpen = false;
    private bool isAnimating = false;


    [SerializeField] private GameObject chestPanel;
    [SerializeField] private UINavigationManager uiNavigationManager;


    private void Start()
    {
        closedRotation = topChest.transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(openEulerAngles);
    }
    public override void OnInteract(PlayerInteractor player)
    {
        Debug.Log("Interacted with chest");
        if (!chestPanel.activeSelf && !isOpen)
        {
            OpenAndClose();
        }
        else
            Debug.Log("Toggling chest");
    }

    public void OpenAndClose()
    {
        if (uiNavigationManager != null)
        {
            uiNavigationManager.onCancel = CloseChestButton; 
        }
        if (chestPanel != null)
        {
            Debug.Log("Activating chest panel");
            chestPanel.SetActive(true);
        }
        chestInventory.content = chestInventory.RefreshItems(chestInventory.content);
        chestInventory.contentChest = chestInventory.RefreshItems(chestInventory.contentChest);
        chestInventory.RefreshContentChestInventory();
        chestInventory.RefreshContentInventory();
        StartCoroutine(OpenChest());
    }

    private IEnumerator OpenChest()
    {
        if (isAnimating || isOpen) yield break;
        isAnimating = true;
        transform.GetComponent<BoxCollider>().enabled = false;
        isOpen = true;
        Opensound.PlayOneShot(Opensound.clip);
        while (Quaternion.Angle(topChest.transform.rotation, openRotation) > 0.1f)
        {
            topChest.transform.rotation = Quaternion.Slerp(
                topChest.transform.rotation,
                openRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }

        // S’assure que la rotation est précise à la fin
        topChest.transform.rotation = openRotation;
        isAnimating = false;
    }

    public void CloseChestButton()
    {
        if (!isOpen || isAnimating)
            return;

        StartCoroutine(CloseChest());
    }


    private IEnumerator CloseChest()
    {
        if (isAnimating || !isOpen)
            yield break;

        isAnimating = true;

        if (uiNavigationManager != null)
            uiNavigationManager.onCancel = null;

        if (chestPanel != null)
            chestPanel.SetActive(false);

        isOpen = false;

        chestInventory.content = chestInventory.RefreshItems(chestInventory.content);
        chestInventory.contentChest = chestInventory.RefreshItems(chestInventory.contentChest);

        //InventorySystem.instance.SetContent(chestInventory.content);
        InventorySystem.instance.RefreshContent();

        while (Quaternion.Angle(topChest.transform.rotation, closedRotation) > 0.1f)
        {
            topChest.transform.rotation = Quaternion.Slerp(
                topChest.transform.rotation,
                closedRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }

        topChest.transform.rotation = closedRotation;
        transform.GetComponent<BoxCollider>().enabled = true;
        isAnimating = false;
    }

}
