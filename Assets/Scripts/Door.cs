using UnityEngine;
using System.Collections;
using System.Linq;

public class Door : InteractableBase
{
    [SerializeField] private int niveauDeVerrouillage = 0; // Niveau de verrouillage de la porte
    [SerializeField] private bool IsUnlockable = true;

    [Header("Sounds")]

    [SerializeField] private AudioSource openDoorSound;
    [SerializeField] private AudioSource lockedDoorSound;
    [SerializeField] private AudioSource unlockedDoorSound;
    [SerializeField] private AudioSource unlockFailSound;


    [SerializeField]
    private GameObject door; // L'objet à faire pivoter

    [SerializeField]
    private float rotationSpeed = 2f;


    [SerializeField] private Vector3 openRotationEuler = new Vector3(0, 0, 90);

    private Quaternion closedRotation;
    private Quaternion openRotation;
    [HideInInspector] public bool isOpen = false;
    [HideInInspector] public bool isAnimating = false;
    public bool isLocked = false;

    [SerializeField] private ItemData keyItem; // L'item de clé requis pour ouvrir la porte

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
            objectType = InteractableObjectType.Door;
        interactUI.SetInteractable(this);
    }
    public void OpenAndCloseDoor()
    {
        if (isLocked)
        {
            lockedDoorSound.PlayOneShot(lockedDoorSound.clip);
            return;
        }
        if (!isOpen && !isAnimating)
        {
            StartCoroutine(OpenDoor());
        }
        else if (isOpen && !isAnimating)
        {
            StartCoroutine(CloseDoor());
        }
    }


    private IEnumerator OpenDoor()
    {
        isOpen = true;
        isAnimating = true;
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
        isAnimating = false;
    }

    private IEnumerator CloseDoor()
    {
        isOpen = false;
        isAnimating = true;
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
        isAnimating = false;
    }

    public void TryToOpenWithKey(ItemData key)
    {
        if (!IsUnlockable)
        {
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
        // Cas 2 : tentative de crochetage avec une clé "improvisée"
        else if (key.attackPoints > 0)
        {
            float chanceDeReussite = Mathf.Clamp01((float)key.attackPoints / (niveauDeVerrouillage + 1));
            float tirage = Random.value;

            Debug.Log($"Chance de réussite : {chanceDeReussite}, tirage : {tirage}");

            if (tirage <= chanceDeReussite)
            {
                DeverrouillerEtOuvrir();
            }
            else
            {
                unlockFailSound.PlayOneShot(unlockFailSound.clip);
            }
            ConsommerCle(key);
        }
        else
        {
            lockedDoorSound.PlayOneShot(lockedDoorSound.clip);
        }
    }
    private void ConsommerCle(ItemData key)
    {
        InventorySystem.instance.RemoveItem(key);
    }
    private void DeverrouillerEtOuvrir()
    {
        isLocked = false;
        unlockedDoorSound.PlayOneShot(unlockedDoorSound.clip);
        OpenAndCloseDoor();
        objectType = InteractableObjectType.Door;
        interactUI.SetInteractable(this);
    }

    public override void OnInteract(PlayerInteractor player)
    {
        if (InventorySystem.instance.KeyIsInInventory(keyItem))
            TryToOpenWithKey(keyItem);
        else
            OpenAndCloseDoor();
    }
}
