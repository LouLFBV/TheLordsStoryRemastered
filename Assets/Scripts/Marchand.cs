using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Marchand : InteractableBase
{
    [Header("Panel")]
    [SerializeField] private GameObject parentsProduits;
    [SerializeField] private ItemData[] produits;
    [SerializeField] private GameObject produitItemPrefab;
    [SerializeField] private Animator animatorPanelProduits;
    private DialogueManager.Speaker currentSpeaker;
    [SerializeField] private GameObject isActive;
    [SerializeField] private TextMeshProUGUI goldPlayer;
    private UIProduitMarchand currentSlotProduit;

    [Header("PNJ")]
    public string namePNJ;
    public string nicknamePNJ;
    public DialogueResponse[] sentences;
    public bool isOnDial;
    private int index = 0;
    private int sentenceIndex = 0;
    private DialogueResponse[] currentDialogue; // tableau actif
    //private Transform playerTransform;
    private PlayerController player;
    //private bool isPlayerInZone;
    private Animator animator;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buySound;



    [HideInInspector] public float inputCooldown = 0.2f; // Temps d'attente après lancement du dialogue
    [HideInInspector] public float dialogueStartTime, dialogueEndTime;

    private void Start()
    {       
        animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
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
            //playerTransform = other.transform;
            //isPlayerInZone = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //isPlayerInZone = false;
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

            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);

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
            currentSpeaker = DialogueManager.Speaker.PNJ;

            DialogueManager.instance.SetSpeakerName(DialogueManager.Speaker.PNJ, namePNJ, nicknamePNJ);
            DialogueManager.instance.ShowLine(dialogueGroup.pnjDialogues[sentenceIndex], DialogueManager.Speaker.PNJ);
            animator.SetInteger("talkIndex", Random.Range(0, 3));
            animator.SetBool("isTalking", true);
        }
        else
        {
            currentSpeaker = DialogueManager.Speaker.Player;
            DialogueManager.instance.SetSpeakerName(DialogueManager.Speaker.Player, "Vous");
            int playerIndex = sentenceIndex - dialogueGroup.pnjDialogues.Length;
            DialogueManager.instance.ShowLine(dialogueGroup.playerResponses[playerIndex], DialogueManager.Speaker.Player);
            animator.SetBool("isTalking", false);
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
    }
    public void EndDiscussion()
    {
        Debug.Log("EndDiscussion Marchand");
        animator.SetBool("isTalking", false);
        dialogueEndTime = Time.time;
        index = 0;

        if (DialogueManager.instance.dialoguePanel.transform.localScale.y > 0)
            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);
    }

    public void OpenProduitsPanel()
    {
        animatorPanelProduits.SetBool("PanelIsOpen", true);
        RefreshProduits();
        isActive.SetActive(true);
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
                    ItemData produit = currentSlotProduit.itemData;

                    if (player.Wallet.CanSpendGold(produit.TotalPrice)
                        && InventorySystem.instance.CanAddItem(produit, produit.PurchaseAmount))
                    {
                        Acheter(produit);
                    }

                    player.Input.UseEquipActionInput();
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

                produitMarchand.nameItem.text = produit.itemName;
                produitMarchand.iconeItem.sprite = produit.visual;
                produitMarchand.priceItem.text = "Prix : " + produit.TotalPrice;
                produitMarchand.stockItemInInventory.text = $"Stock : {currentStock}";

                // On vide et on met UN SEUL listener (Acheter simple par exemple sur le bouton)
                produitMarchand.buyButton.onClick.RemoveAllListeners();
                produitMarchand.buyButton.onClick.AddListener(() =>
                {
                    if (CanBuy(produit))
                    {
                        Acheter(produit);
                    }
                });
                VerfifButtonAcheter(produit, produitMarchand.buyButton);

                produitMarchand.actionButtonsGroup.SetActive(false);
            }
        }
    }

    private void Acheter(ItemData produit)
    {
        if (!player.Wallet.CanSpendGold(produit.TotalPrice))
            return;

        if (!InventorySystem.instance.CanAddItem(produit, produit.PurchaseAmount))
            return;

        player.Wallet.SpendGold(produit.TotalPrice);
        if (audioSource != null && buySound != null) audioSource.PlayOneShot(buySound);
        InventorySystem.instance.AddItem(produit, produit.PurchaseAmount);

        RefreshProduits();
    }

    private void VerfifButtonAcheter(ItemData produit, Button buyButton)
    {
        Image buttonImage = buyButton.GetComponent<Image>();

        bool canBuy = CanBuy(produit);

        //buttonImage.color = canBuy ? Color.green : Color.red;
        buyButton.interactable = canBuy;
    }
    private bool CanBuy(ItemData produit)
    {
        return
            player.Wallet.CanSpendGold(produit.TotalPrice) &&
            InventorySystem.instance.CanAddItem(produit, produit.PurchaseAmount);
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
