using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using static UnityEditor.Progress;

public class PNJAcheteur : PNJParent
{
    [Header("Pourcentage de Rachat")]
    [SerializeField] private float pourcentageDeRachat = 0.9f;

    private UIProduitMarchand currentSlotProduit;
    [SerializeField] private TextMeshProUGUI goldPlayer;
    public override void OnInteract(PlayerInteractor player)
    {
        if (isOnDial && Time.time - dialogueStartTime > inputCooldown && !animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if (!DialogueManager.instance.SkipOrFinish(currentSpeaker) && !DialogueManager.instance.inDelay)
                StartDialogue(sentences);
        }
        else if (!animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            StartDialogue(sentences);
            SetTargeted(false, playerTransform);
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


    private void Update()
    {
        if (animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if (currentSlotProduit != null)
            {
                // Vendre 1 unité (Touche E)
                if (player.Input.EquipActionPressed)
                {
                    currentSlotProduit.buyButton.onClick.Invoke(); // Utilise la bonne méthode (Vendre, VendreWeapons, etc.)
                    player.Input.UseEquipActionInput();
                }

                // Vendre TOUT le stock (Touche F / UseAction)
                if (player.Input.UseActionPressed)
                {
                    // On vérifie si le bouton "Vendre tout" est actif sur ce slot
                    if (currentSlotProduit.fillStockButton != null && currentSlotProduit.fillStockButton.gameObject.activeSelf)
                    {
                        currentSlotProduit.fillStockButton.onClick.Invoke();
                    }
                    else
                    {
                        // S'il n'y a qu'un exemplaire, on vend juste celui-là
                        currentSlotProduit.buyButton.onClick.Invoke();
                    }
                    player.Input.UseUseActionInput();
                }
            }

            // Fermeture
            if (player.Input.CancelPressed || player.Input.CloseMenuPressed)
            {
                EndCommerce();
                player.Input.UseCancelInput();
                player.Input.UseCloseMenuInput();
            }
        }
    }
    public void StartDialogue(List<DialogueResponse> sentence)
    {
        if (index == 0 && leghthSentences == sentences.Count)
        {
            if (VerifIfEmpty())
            {
                sentence.Add(
                    new DialogueResponse
                    {
                        pnjDialogues = new string[] { "Oh mais je vois que vous n'avez rien à vendre. Revenez me voir lorsque vous aurez quelque chose pour moi !" },
                        playerResponses = new string[] { "D'accord, à une prochaine fois !" }
                    }
                    );
            }
            else
            {
                sentence.Add(
                new DialogueResponse
                {
                    pnjDialogues = new string[] { },
                    playerResponses = new string[] { "Proposez moi vos prix !" }
                }
                );
            }
        }
        if (!isOnDial)
        {
            StartCoroutine(RotateTowardsPlayer());
            isOnDial = true;

            player.RequestedPanelType = UIPanelType.Dialogue;
            player.StateMachine.ChangeState(PlayerStateType.UI);
            DialogueManager.instance.textName.text = namePNJ;

            index = 0;
            dialogueStartTime = Time.time; // Enregistrer le temps de début du dialogue
            currentDialogue = sentence;
        }
        else if (!animatorPanelProduits.GetBool("PanelIsOpen") && index >= sentences.Count)
        {
            RefreshProduits();
            if (!VerifIfEmpty())
            {
                OpenProduitsPanel();
            }
            EndDiscussion();
            return;
        }

        var dialogueGroup = currentDialogue[index];

        // Affiche le dialogue PNJ ou la réponse du joueur selon l'index
        if (sentenceIndex < dialogueGroup.pnjDialogues.Length)
        {
            if (!firstDialoguePnjDone)
            {
                DialogueManager.instance.ShowLine(dialogueGroup.pnjDialogues[sentenceIndex], DialogueManager.Speaker.PNJ, 0.5f);
                DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);
                firstDialoguePnjDone = true;
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
                DialogueManager.instance.ShowLine(dialogueGroup.playerResponses[playerIndex], DialogueManager.Speaker.Player, 0.5f);
                firstDialoguePlayerDone = true;
                DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePlayerPanel);
            }
            else
                DialogueManager.instance.ShowLine(dialogueGroup.playerResponses[playerIndex], DialogueManager.Speaker.Player);
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

    // GESTION DES PRODUITS

    private void RefreshProduits()
    {
        UpdateGoldPlayerText();
        currentSlotProduit = null;

        foreach (Transform child in parentsProduits.transform)
        {
            Destroy(child.gameObject);
        }

        // Liste pour garder une trace des items déjà affichés dans le shop
        List<ItemData> itemsTraites = new List<ItemData>();

        // 1. INVENTAIRE (Cumul)
        foreach (ItemInInventory produit in InventorySystem.instance.GetContent())
        {
            if (!itemsTraites.Contains(produit.itemData))
            {
                VerifItemData(produit.itemData, Vendre);
                itemsTraites.Add(produit.itemData);
            }
        }

        // 2. ARMES (Cumul)
        foreach (ItemInInventory produit in PaletteSystem.instance.slotManager.weapons)
        {
            if (produit.itemData != null && !itemsTraites.Contains(produit.itemData))
            {
                VerifItemData(produit.itemData, VendreWeapons);
                itemsTraites.Add(produit.itemData);
            }
        }

        // 3. OBJETS RAPIDES (Cumul)
        foreach (ItemInInventory produit in PaletteSystem.instance.slotManager.objects)
        {
            if (produit.itemData != null && !itemsTraites.Contains(produit.itemData))
            {
                VerifItemData(produit.itemData, VendreObjects);
                itemsTraites.Add(produit.itemData);
            }
        }

        // 4. ÉQUIPEMENT (Cumul)
        // On vérifie les flèches
        ItemData arrows = EquipmentSystem.instance.arrowItemInInventory.itemData;
        if (arrows != null && !itemsTraites.Contains(arrows))
        {
            VerifItemData(arrows, VendreWeapons);
            itemsTraites.Add(arrows);
        }

        // On vérifie chaque slot d'armure
        VerifSlotEquipement(EquipmentSystem.instance.headSlot.item, itemsTraites);
        VerifSlotEquipement(EquipmentSystem.instance.chestSlot.item, itemsTraites);
        VerifSlotEquipement(EquipmentSystem.instance.handsSlot.item, itemsTraites);
        VerifSlotEquipement(EquipmentSystem.instance.legsSlot.item, itemsTraites);
        VerifSlotEquipement(EquipmentSystem.instance.feetSlot.item, itemsTraites);

        if (VerifIfEmpty())
        {
            EndCommerce();
        }
    }

    // Petite méthode d'aide pour l'équipement
    private void VerifSlotEquipement(ItemData item, List<ItemData> liste)
    {
        if (item != null && !liste.Contains(item))
        {
            VerifItemData(item, VendreEquipment);
            liste.Add(item);
        }
    }

    private void VerifItemData(ItemData item, Action<ItemData> methode)
    {
        if (item == null || item.prix <= 0) return;

        GameObject produitItem = Instantiate(produitItemPrefab, parentsProduits.transform);

        if (produitItem.TryGetComponent<UIProduitMarchand>(out var slot))
        {
            // 1. Initialisation de base
            slot.SetupPNJAcheteur(item, this);
            slot.nameItem.text = item.itemName;
            slot.iconeItem.sprite = item.visual;

            // 2. Calcul des prix de rachat
            int prixUnitaireRachat = Mathf.RoundToInt(item.prix * pourcentageDeRachat);
            slot.priceItem.text = prixUnitaireRachat.ToString();

            // 3. Gestion du stock
            int currentStock = InventorySystem.instance.GetItemCount(item);
            if (slot.stockItemInInventory != null)
                slot.stockItemInInventory.text = $"Stock : {currentStock}";

            // 4. Bouton Vendre 1 unité
            slot.buyButton.onClick.RemoveAllListeners();
            slot.buyButton.onClick.AddListener(() => methode(item));

            // 5. Bouton Vendre TOUT le stock (si tu as un bouton dédié comme fillStockButton)
            if (slot.fillStockButton != null)
            {
                if (currentStock > 1)
                {
                    slot.fillStockButton.gameObject.SetActive(true);
                    int prixTotalRachat = prixUnitaireRachat * currentStock;
                    slot.priceFillStock.text = prixTotalRachat.ToString();

                    slot.fillStockButton.onClick.RemoveAllListeners();
                    // On boucle sur la méthode de vente pour vendre tout le stock
                    slot.fillStockButton.onClick.AddListener(() => {
                        for (int i = 0; i < currentStock; i++) methode(item);
                    });
                }
                else
                {
                    slot.fillStockButton.gameObject.SetActive(false);
                }
            }

            slot.actionButtonsGroup.SetActive(false);
        }
    }
    private void Vendre(ItemData produit)
    {
        PlayerController.Instance.Wallet.AddGold(Mathf.RoundToInt(produit.prix * pourcentageDeRachat));
        InventorySystem.instance.RemoveItem(produit);
        RefreshProduits();
    }
    private void VendreObjects(ItemData produit)
    {
        PlayerController.Instance.Wallet.AddGold(Mathf.RoundToInt(produit.prix * pourcentageDeRachat));
        if (produit == PaletteSystem.instance.slotManager.objects[0].itemData)
            PaletteSystem.instance.equipmentManager.RemoveObject(1);
        else if (produit == PaletteSystem.instance.slotManager.objects[1].itemData)
            PaletteSystem.instance.equipmentManager.RemoveObject(2);
        InventorySystem.instance.RemoveItem(produit);
        RefreshProduits();
    }
    private void VendreWeapons(ItemData produit)
    {
        PlayerController.Instance.Wallet.AddGold(Mathf.RoundToInt(produit.prix * pourcentageDeRachat));
        if (produit == PaletteSystem.instance.slotManager.weapons[0].itemData)
            PaletteSystem.instance.equipmentManager.DesequipWeapon(1);
        else if (produit == PaletteSystem.instance.slotManager.weapons[1].itemData)
            PaletteSystem.instance.equipmentManager.DesequipWeapon(2);
        InventorySystem.instance.RemoveItem(produit);
        RefreshProduits();
    }

    private void VendreEquipment(ItemData produit)
    {
        PlayerController.Instance.Wallet.AddGold(Mathf.RoundToInt(produit.prix * pourcentageDeRachat));
        EquipmentSystem.instance.DesequipEquipment(produit.equipmentType);
        InventorySystem.instance.RemoveItem(produit);
        RefreshProduits();
    }

    private bool VerifIfEmpty()
    {
        return PaletteSystem.instance.slotManager.weapons[0].itemData == null && PaletteSystem.instance.slotManager.weapons[1].itemData == null &&
                EquipmentSystem.instance.headSlot.item == null && EquipmentSystem.instance.chestSlot.item == null &&
                EquipmentSystem.instance.handsSlot.item == null && EquipmentSystem.instance.legsSlot.item == null &&
                EquipmentSystem.instance.feetSlot.item == null && InventorySystem.instance.GetContent().Count == 0;
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
