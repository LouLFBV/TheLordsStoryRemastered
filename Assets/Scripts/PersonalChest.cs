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

    [SerializeField] private GameObject chestPanel;

    private BoxCollider chestCollider;

    private Coroutine activeChestCoroutine = null;

    private void Start()
    {
        closedRotation = topChest.transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(openEulerAngles);
        chestCollider = GetComponent<BoxCollider>();
    }

    public override void OnInteract(PlayerInteractor player)
    {
        Debug.Log("Interacted with chest");

        // Si le coffre est fermé (ou en train de se fermer), on l'ouvre
        if (!isOpen)
        {
            OpenChestSequence();
        }
        else // Si le coffre est ouvert (ou en train d'ouvrir), on le ferme
        {
            CloseChestButton();
        }
    }

    private void Update()
    {
        if (chestPanel != null && chestPanel.activeInHierarchy)
        {
            if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
            {
                if (PlayerController.Instance.Input.CloseMenuPressed || PlayerController.Instance.Input.MenuPressed || PlayerController.Instance.Input.CancelPressed)
                {
                    CloseChestButton();
                    PlayerController.Instance.Input.UseCloseMenuInput();
                }
            }
        }
    }

    public void OpenChestSequence()
    {
        if (activeChestCoroutine != null)
        {
            StopCoroutine(activeChestCoroutine);
        }

        if (chestPanel != null)
        {
            Debug.Log("Activating chest panel");
            chestPanel.SetActive(true);
        }

        if (chestInventory != null)
        {
            chestInventory.RefreshContentChestInventory();
            chestInventory.RefreshContentPlayerInventory();
        }

        // On lance et on stocke la coroutine d'ouverture
        activeChestCoroutine = StartCoroutine(OpenChest());
    }

    private IEnumerator OpenChest()
    {
        PlayerController.Instance.RequestedPanelType = UIPanelType.Dialogue;
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);

        isOpen = true;

        if (chestCollider != null) chestCollider.enabled = false;

        if (Opensound != null && Opensound.clip != null)
        {
            Opensound.PlayOneShot(Opensound.clip);
        }

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
        activeChestCoroutine = null; // On vide la référence quand c'est fini
    }

    public void CloseChestButton()
    {
        if (!isOpen) return;

        Debug.Log("Closing chest");

        if (activeChestCoroutine != null)
        {
            StopCoroutine(activeChestCoroutine);
        }

        // On lance et on stocke la coroutine de fermeture
        activeChestCoroutine = StartCoroutine(CloseChest());
    }

    private IEnumerator CloseChest()
    {
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.Idle);
        isOpen = false;

        if (chestPanel != null)
            chestPanel.SetActive(false);

        if (PaletteSystem.instance != null && PaletteSystem.instance.slotManager != null)
        {
            PaletteSystem.instance.slotManager.UpdateImageSeleted();
        }

        // Le Slerp va partir fluidement de l'angle actuel (même s'il était à moitié ouvert !)
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

        if (chestCollider != null) chestCollider.enabled = true;

        activeChestCoroutine = null; // On vide la référence quand c'est fini
    }
}