using UnityEngine;
using System.Collections;
using System.Linq;

public class Door : InteractableBase
{
    [SerializeField] private bool IsUnlockable = true;

    [Header("Sounds")]
    [SerializeField] private AudioSource openDoorSound;
    [SerializeField] private AudioSource lockedDoorSound;
    [SerializeField] private AudioSource unlockedDoorSound;

    [SerializeField] private GameObject door; // L'objet à faire pivoter
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private Vector3 openRotationEuler = new Vector3(0, 0, 90);

    private Quaternion closedRotation;
    private Quaternion openRotation;

    [HideInInspector] public bool isOpen = false;
    public bool isLocked = false;

    [SerializeField] private ItemData keyItem; // L'item de clé requis pour ouvrir la porte

    private Coroutine activeDoorCoroutine = null;

    private void Start()
    {
        if (door != null)
        {
            closedRotation = door.transform.rotation;
            openRotation = closedRotation * Quaternion.Euler(openRotationEuler);
        }

        if (isLocked)
        {
            if (IsUnlockable)
                objectType = InteractableObjectType.Key;
            else
                objectType = InteractableObjectType.Locked;
        }
        else
        {
            objectType = InteractableObjectType.Door;
        }

        if (interactUI != null)
            interactUI.SetInteractable(this);
    }

    public void OpenAndCloseDoor()
    {
        if (isLocked)
        {
            if (lockedDoorSound != null && lockedDoorSound.clip != null)
                lockedDoorSound.PlayOneShot(lockedDoorSound.clip);
            return;
        }

        // Si la porte est déjà en train de bouger, on stoppe son animation actuelle
        if (activeDoorCoroutine != null)
        {
            StopCoroutine(activeDoorCoroutine);
        }

        // On inverse le mouvement en fonction de l'état actuel
        if (!isOpen)
        {
            activeDoorCoroutine = StartCoroutine(OpenDoor());
        }
        else
        {
            activeDoorCoroutine = StartCoroutine(CloseDoor());
        }
    }

    private IEnumerator OpenDoor()
    {
        isOpen = true;

        if (openDoorSound != null && openDoorSound.clip != null)
            openDoorSound.PlayOneShot(openDoorSound.clip);

        while (Quaternion.Angle(door.transform.rotation, openRotation) > 0.1f)
        {
            door.transform.rotation = Quaternion.Slerp(
                door.transform.rotation,
                openRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }

        door.transform.rotation = openRotation;
        activeDoorCoroutine = null; // Libère la référence une fois l'animation terminée
    }

    private IEnumerator CloseDoor()
    {
        isOpen = false;

        if (openDoorSound != null && openDoorSound.clip != null)
            openDoorSound.PlayOneShot(openDoorSound.clip);

        while (Quaternion.Angle(door.transform.rotation, closedRotation) > 0.1f)
        {
            door.transform.rotation = Quaternion.Slerp(
                door.transform.rotation,
                closedRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }

        door.transform.rotation = closedRotation;
        activeDoorCoroutine = null; // Libère la référence une fois l'animation terminée
    }

    public void TryToOpenWithKey(ItemData key)
    {
        if (!IsUnlockable)
        {
            if (lockedDoorSound != null && lockedDoorSound.clip != null)
                lockedDoorSound.PlayOneShot(lockedDoorSound.clip);
            return;
        }
        else if (!isLocked)
        {
            OpenAndCloseDoor();
            return;
        }
        // Cas 1 : la bonne clé est utilisée
        else if (key == keyItem)
        {
            ConsommerCle(key);
            DeverrouillerEtOuvrir();
        }
        else
        {
            if (lockedDoorSound != null && lockedDoorSound.clip != null)
                lockedDoorSound.PlayOneShot(lockedDoorSound.clip);
        }
    }

    private void ConsommerCle(ItemData key)
    {
        if (InventorySystem.instance != null)
            InventorySystem.instance.RemoveItem(key);
    }

    private void DeverrouillerEtOuvrir()
    {
        isLocked = false;

        if (unlockedDoorSound != null && unlockedDoorSound.clip != null)
            unlockedDoorSound.PlayOneShot(unlockedDoorSound.clip);

        OpenAndCloseDoor();
        objectType = InteractableObjectType.Door;

        if (interactUI != null)
            interactUI.SetInteractable(this);
    }

    public override void OnInteract(PlayerInteractor player)
    {
        if (keyItem != null && InventorySystem.instance != null && InventorySystem.instance.KeyIsInInventory(keyItem))
        {
            Debug.Log("Player has the key, trying to open the door with it.");
            TryToOpenWithKey(keyItem);
        }
        else
        {
            Debug.Log("Player does not have the key, trying to open the door normally.");
            OpenAndCloseDoor();
        }
    }
}