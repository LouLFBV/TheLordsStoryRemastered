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

    private BoxCollider chestCollider;

    private void Start()
    {
        closedRotation = topChest.transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(openEulerAngles);

        // Cache du composant pour éviter le transform.GetComponent<> dans les Coroutines
        chestCollider = GetComponent<BoxCollider>();
    }

    public override void OnInteract(PlayerInteractor player)
    {
        Debug.Log("Interacted with chest");

        // Sécurité : si on est en train d'animer, on ignore l'input
        if (isAnimating) return;

        // AMÉLIORATION : Permet d'ouvrir ET de fermer le coffre avec la touche d'interaction
        if (!isOpen)
        {
            OpenChestSequence();
        }
        else
        {
            CloseChestButton();
        }
    }

    private void Update()
    {
        if (PlayerController.Instance.Input.CloseMenuPressed && isOpen)
        {
            CloseChestButton();
            PlayerController.Instance.Input.UseCloseMenuInput();
        }
    }

    public void OpenChestSequence()
    {
        if (chestPanel != null)
        {
            Debug.Log("Activating chest panel");
            chestPanel.SetActive(true);
        }

        // Le coffre lit et peuple les 4 listes (Craft & Ressources pour le Coffre et le Joueur)
        if (chestInventory != null)
        {
            chestInventory.RefreshContentChestInventory();
            chestInventory.RefreshContentPlayerInventory();
        }

        StartCoroutine(OpenChest());
    }

    private IEnumerator OpenChest()
    {
        PlayerController.Instance.RequestedPanelType = UIPanelType.Dialogue;
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);
        if (isAnimating || isOpen) yield break;
        isAnimating = true;
        isOpen = true;

        // Désactive le collider pour éviter qu'on puisse réinteragir pendant l'animation physique
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
        isAnimating = false;
    }

    public void CloseChestButton()
    {
        if (!isOpen || isAnimating)
            return;

        Debug.Log("Closing chest");
        StartCoroutine(CloseChest());
    }

    private IEnumerator CloseChest()
    {
        if (isAnimating || !isOpen)
            yield break;

        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.Idle);
        isAnimating = true;
        isOpen = false;

        if (chestPanel != null)
            chestPanel.SetActive(false);

        // --- CORRECTION ET SYNCHRONISATION DES SYSTÈMES ---
        // On demande à ton PaletteSystem ou InventorySystem de mettre à jour son visuel (HUD principal de jeu)
        // en se basant sur ce qui est maintenant dans le portefeuille du joueur
        if (PaletteSystem.instance != null && PaletteSystem.instance.slotManager != null)
        {
            PaletteSystem.instance.slotManager.UpdateImageSeleted();
        }

        // Si ton InventorySystem possède une fonction pour synchroniser ses données globales, appelle-la ici.
        // Sinon, la modification est déjà enregistrée dans les listes Player de 'chestInventory' pendant les transferts.

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

        // On réactive le collider pour permettre une future ouverture
        if (chestCollider != null) chestCollider.enabled = true;

        isAnimating = false;
    }
}