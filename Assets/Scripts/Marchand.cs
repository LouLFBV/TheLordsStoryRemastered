using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Marchand : InteractableBase
{
    [Header("Panel")]
    [SerializeField] private GameObject parentsProduits;
    [SerializeField] private GameObject panelProduits;
    [SerializeField] private ItemData[] produits;
    [SerializeField] private GameObject produitItemPrefab;
    [SerializeField] private Animator animatorPanelProduits;
    private DialogueManager.Speaker currentSpeaker;
    [SerializeField] private GameObject isActive;
    [SerializeField] private TextMeshProUGUI goldPlayer;
    private UIProduitMarchand currentSlotProduit;

    [Header("PNJ")]
    public string namePNJ;
    public DialogueResponse[] sentences;
    public bool isOnDial;
    private int index = 0;
    private int sentenceIndex = 0;
    private DialogueResponse[] currentDialogue; // tableau actif
    private bool firstDialoguePlayerDone = false, firstDialoguePnjDone = false;
    private Transform playerTransform;
    private PlayerController player;
    private bool isPlayerInZone;
    private Animator animator;


    [HideInInspector] public float inputCooldown = 0.2f; // Temps d'attente après lancement du dialogue
    [HideInInspector] public float dialogueStartTime, dialogueEndTime;


    [SerializeField] private UINavigationManager navManager;

    private void Start()
    {       
        animator = GetComponent<Animator>();
    }
    public override void OnInteract(PlayerInteractor player)
    {
        Debug.Log("Interact Marchand");
        if (isOnDial && Time.time - dialogueStartTime > inputCooldown && !animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if (!DialogueManager.instance.SkipOrFinish(currentSpeaker) && !DialogueManager.instance.inDelay)
                StartDialogue(sentences);
        }
        else if (!isOnDial)
        {
            StartDialogue(sentences);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            playerTransform = other.transform;
            isPlayerInZone = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            player = null;
        }
    }

    // GESTION DU DIALOGUE
    public void StartDialogue(DialogueResponse[] sentence)
    {
        if (!isOnDial)
        {
            isOnDial = true;
            player.RequestedPanelType = UIPanelType.Dialogue;
            player.StateMachine.ChangeState(PlayerStateType.UI);

            DialogueManager.instance.textName.text = namePNJ;

            index = 0;
            dialogueStartTime = Time.time; // Enregistrer le temps de début du dialogue
            currentDialogue = sentence;
        }
        if (index >= currentDialogue.Length && !animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            OpenProduitsPanel();
            EndDiscussion();
            return;
        }


        var dialogueGroup = currentDialogue[index];

        // Affiche le dialogue PNJ ou la réponse du joueur selon l'index
        if (sentenceIndex < dialogueGroup.pnjDialogues.Length)
        {
            if (!firstDialoguePnjDone)
            {
                DialogueManager.instance.ShowLine(dialogueGroup.pnjDialogues[sentenceIndex], DialogueManager.Speaker.PNJ, 0.75f);
                firstDialoguePnjDone = true;
                DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);
            }
            else
                DialogueManager.instance.ShowLine(dialogueGroup.pnjDialogues[sentenceIndex], DialogueManager.Speaker.PNJ);
            animator.SetBool("isTalking", true);
            currentSpeaker = DialogueManager.Speaker.PNJ;
        }
        else
        {
            int playerIndex = sentenceIndex - dialogueGroup.pnjDialogues.Length;
            if (!firstDialoguePlayerDone)
            {
                DialogueManager.instance.ShowLine(dialogueGroup.playerResponses[playerIndex], DialogueManager.Speaker.Player, 0.75f);
                firstDialoguePlayerDone = true;
                DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePlayerPanel);
            }
            else
                DialogueManager.instance.ShowLine(dialogueGroup.playerResponses[sentenceIndex], DialogueManager.Speaker.Player);
            animator.SetBool("isTalking", false);
            currentSpeaker = DialogueManager.Speaker.Player;
        }
        sentenceIndex++;

        // Si on a fini toutes les lignes du groupe, passe au groupe suivant
        if (sentenceIndex >= dialogueGroup.pnjDialogues.Length + dialogueGroup.playerResponses.Length)
        {
            sentenceIndex = 0;
            index++;
        }
    }


    public void EndCommerce()
    {
        isOnDial = false;
        dialogueEndTime = Time.time;
        animatorPanelProduits.SetBool("PanelIsOpen", false);
        isActive.SetActive(false);
        player.StateMachine.ChangeState(PlayerStateType.Idle);
        if (navManager != null)
        {
            navManager.onCancel = null;
        }
    }
    public void EndDiscussion()
    {
        Debug.Log("EndDiscussion Marchand");
        firstDialoguePnjDone = false;
        firstDialoguePlayerDone = false;
        animator.SetBool("isTalking", false);
        dialogueEndTime = Time.time;
        index = 0;

        if (DialogueManager.instance.dialoguePanel.transform.localScale.y > 0)
            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);

        if (DialogueManager.instance.dialoguePlayerPanel.transform.localScale.y > 0)
            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePlayerPanel);
    }

    public void OpenProduitsPanel()
    {
        animatorPanelProduits.SetBool("PanelIsOpen", true);
        RefreshProduits();
        isActive.SetActive(true);
        if (navManager != null)
        {
            navManager.onCancel = EndCommerce;
        }
    }
    // GESTION DES PRODUITS
    private void Update()
    {
        // On ne vérifie les touches que si le panel est ouvert et qu'un slot est survolé
        if (animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if( currentSlotProduit != null)
            {
                // Achat simple (Touche E / EquipAction)
                if (player.Input.EquipActionPressed)
                {
                    Acheter(currentSlotProduit.itemData);
                    player.Input.UseEquipActionInput(); // Consomme l'input
                }
                // Remplir le stock (Touche 1 / UseAction)
                else if (player.Input.UseActionPressed)
                {
                    int remainingToFill = currentSlotProduit.itemData.maxStack - InventorySystem.instance.GetItemCount(currentSlotProduit.itemData);
                    if (remainingToFill > 0)
                    {
                        Acheter(currentSlotProduit.itemData, remainingToFill);
                    }
                    player.Input.UseUseActionInput();
                }
            }
            if (player.Input.CancelPressed || player.Input.CloseMenuPressed)
            {
                EndCommerce();
                player.Input.UseCancelInput();
                player.Input.UseCloseMenuInput();
            }
        }
    }

    // Version corrigée de RefreshProduits
    private void RefreshProduits()
    {
        UpdateGoldPlayerText();
        foreach (Transform child in parentsProduits.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData produit in produits)
        {
            GameObject produitItem = Instantiate(produitItemPrefab, parentsProduits.transform);

            if (produitItem.TryGetComponent<UIProduitMarchand>(out var produitMarchand))
            {
                // IMPORTANT : On initialise le slot
                produitMarchand.Setup(produit, this);

                int currentStock = InventorySystem.instance.GetItemCount(produit);
                int remainingStock = produit.maxStack - currentStock;

                produitMarchand.nameItem.text = produit.itemName;
                produitMarchand.iconeItem.sprite = produit.visual;
                produitMarchand.priceItem.text = "Prix : " + produit.prix;
                produitMarchand.stockItemInInventory.text = $"Stock : {currentStock}/{produit.maxStack}";
                produitMarchand.priceFillStock.text = (remainingStock * produit.prix).ToString();

                // On vide et on met UN SEUL listener (Acheter simple par exemple sur le bouton)
                produitMarchand.buyButton.onClick.RemoveAllListeners();
                produitMarchand.buyButton.onClick.AddListener(() => Acheter(produit));
                VerfifButtonAcheter(produit, produitMarchand.buyButton);

                produitMarchand.fillStockButton.onClick.RemoveAllListeners();
                produitMarchand.fillStockButton.onClick.AddListener(() => Acheter(produit, remainingStock));
                VerfifButtonAcheter(produit, produitMarchand.fillStockButton, remainingStock);

                produitMarchand.actionButtonsGroup.SetActive(false);
            }
        }
    }

    private void Acheter(ItemData produit, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {        
            if (player.Wallet.SpendGold(produit.prix) && !InventorySystem.instance.IsFullEquipment())
            {
                InventorySystem.instance.AddItem(produit);
            }
        }
        RefreshProduits();
    }

    private void VerfifButtonAcheter(ItemData produit, Button buyButton, int amount = 1)
    {
        Image buttonImage = buyButton.GetComponent<Image>();
        if (player.Wallet.CanSpendGold(produit.prix*amount)  && VerifInInventoryAndPalette(produit) && InventorySystem.instance.GetItemCount(produit) < produit.maxStack)
        {
            buttonImage.color = Color.green; // Set button color to white if affordable
            buyButton.interactable = true;
        }
        else
        {
            buttonImage.color = Color.red; // Set button color to red if not affordable
            buyButton.interactable = false;
        }
    }

    private bool VerifInInventoryAndPalette(ItemData produit)
    {
        foreach (ItemInInventory item in InventorySystem.instance.GetContent())
        {
            if ((item.itemData.itemType == ItemType.Equipment  || item.itemData.itemType == ItemType.Key || item.itemData.itemType == ItemType.QuestItem) && item.itemData == produit)
            {
                return false; // Item is already in the inventory
            }
        }
        if (PaletteSystem.instance.slotManager.weapons[0].itemData == produit || PaletteSystem.instance.slotManager.weapons[1].itemData)
        {
            return false; // Item is already equipped in weapon slot 1
        }
        else if (PaletteSystem.instance.slotManager.objects[0].itemData == produit  && (PaletteSystem.instance.slotManager.objects[0].itemData.itemType == ItemType.Equipment ||
            PaletteSystem.instance.slotManager.objects[0].itemData.itemType == ItemType.Key ||
            PaletteSystem.instance.slotManager.objects[0].itemData.itemType == ItemType.QuestItem) 
            ||
            (PaletteSystem.instance.slotManager.objects[1].itemData == produit && (PaletteSystem.instance.slotManager.objects[1].itemData.itemType == ItemType.Equipment ||
            PaletteSystem.instance.slotManager.objects[1].itemData.itemType == ItemType.Key ||
            PaletteSystem.instance.slotManager.objects[1].itemData.itemType == ItemType.QuestItem)))
        {
            return false; // Item is already equipped in armor slots
        }
        return true; // Item is not in the inventory
    }


    // Cette méthode sera appelée par UIProduitMarchand
    public void SetCurrentHoveredItem(UIProduitMarchand slot)
    {
        currentSlotProduit = slot;
    }

    private void UpdateGoldPlayerText()
    {
        goldPlayer.text = player.Wallet.GetGoldAmount().ToString();
    }
}
