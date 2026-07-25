using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System;

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

    private void Update()
    {
        if (animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if (player.Input.MenuPressed || player.Input.CloseMenuPressed || player.Input.CancelPressed)
            {
                EndCommerce();
                player.Input.UseMenuInput();
                player.Input.UseCloseMenuInput();
            }

            if (currentSlotProduit != null)
            {
                // Vendre 1 unité (Touche E)
                if (player.Input.EquipActionPressed)
                {
                    currentSlotProduit.buyButton.onClick.Invoke();
                    player.Input.UseEquipActionInput();
                }

                // Vendre TOUT le stock (Touche F)
                if (player.Input.UseActionPressed)
                {
                    if (currentSlotProduit.fillStockButton != null && currentSlotProduit.fillStockButton.gameObject.activeSelf)
                    {
                        currentSlotProduit.fillStockButton.onClick.Invoke();
                    }
                    else
                    {
                        currentSlotProduit.buyButton.onClick.Invoke();
                    }
                    player.Input.UseUseActionInput();
                }
            }
        }
    }

    public void StartDialogue(List<DialogueResponse> sentence)
    {
        if (index == 0 && leghthSentences == sentences.Count)
        {
            if (VerifIfEmpty())
            {
                sentence.Add(new DialogueResponse
                {
                    pnjDialogues = new string[] { "Oh mais je vois que vous n'avez rien à vendre. Revenez me voir lorsque vous aurez quelque chose pour moi !" },
                    playerResponses = new string[] { "D'accord, à une prochaine fois !" }
                });
            }
            else
            {
                sentence.Add(new DialogueResponse
                {
                    pnjDialogues = new string[] { },
                    playerResponses = new string[] { "Proposez moi vos prix !" }
                });
            }
        }

        if (!isOnDial)
        {
            StartCoroutine(RotateTowardsPlayer());
            isOnDial = true;

            player.RequestedPanelType = UIPanelType.Dialogue;
            player.StateMachine.ChangeState(PlayerStateType.UI);
            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);

            index = 0;
            dialogueStartTime = Time.time;
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

        if (sentenceIndex < dialogueGroup.pnjDialogues.Length)
        {
            currentSpeaker = DialogueManager.Speaker.PNJ;
            DialogueManager.instance.SetSpeakerName(DialogueManager.Speaker.PNJ, namePNJ, nicknamePNJ);
            DialogueManager.instance.ShowLine(dialogueGroup.pnjDialogues[sentenceIndex], DialogueManager.Speaker.PNJ);
            animator.SetInteger("talkIndex", UnityEngine.Random.Range(0, 3));
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

        if (sentenceIndex >= dialogueGroup.pnjDialogues.Length + dialogueGroup.playerResponses.Length)
        {
            sentenceIndex = 0;
            index++;
        }
    }

    // ==================== GESTION DES PRODUITS ====================

    private void RefreshProduits()
    {
        UpdateGoldPlayerText();
        currentSlotProduit = null;

        foreach (Transform child in parentsProduits.transform)
        {
            Destroy(child.gameObject);
        }

        // 1. Récupération de TOUS les objets vendables du joueur
        List<ItemData> tousLesObjets = new List<ItemData>();

        // Inventaire
        foreach (ItemInInventory produit in InventorySystem.instance.GetContent())
        {
            if (produit.itemData != null && produit.itemData.isVendable)
                tousLesObjets.Add(produit.itemData);
        }

        // Armes Palette
        foreach (ItemInInventory produit in PaletteSystem.instance.slotManager.weapons)
        {
            if (produit.itemData != null && produit.itemData.isVendable)
                tousLesObjets.Add(produit.itemData);
        }

        // Objets Rapides Palette
        foreach (ItemInInventory produit in PaletteSystem.instance.slotManager.objects)
        {
            if (produit.itemData != null && produit.itemData.isVendable)
                tousLesObjets.Add(produit.itemData);
        }

        // Flèches
        ItemData arrows = EquipmentSystem.instance.arrowItemInInventory.itemData;
        if (arrows != null && arrows.isVendable)
            tousLesObjets.Add(arrows);

        // Armures équipées
        AddIfVendable(tousLesObjets, EquipmentSystem.instance.headSlot.item);
        AddIfVendable(tousLesObjets, EquipmentSystem.instance.chestSlot.item);
        AddIfVendable(tousLesObjets, EquipmentSystem.instance.handsSlot.item);
        AddIfVendable(tousLesObjets, EquipmentSystem.instance.legsSlot.item);
        AddIfVendable(tousLesObjets, EquipmentSystem.instance.feetSlot.item);

        // 2. TRI : On regroupe par NOM puis par NIVEAU d'amélioration croissant !
        tousLesObjets.Sort((a, b) =>
        {
            int nameCompare = string.Compare(a.itemName, b.itemName, StringComparison.Ordinal);
            if (nameCompare != 0) return nameCompare;
            return a.levelAmelioration.CompareTo(b.levelAmelioration);
        });

        // 3. Affichage dans l'interface (une case par ID + Niveau)
        Dictionary<string, ItemData> itemsTraites = new Dictionary<string, ItemData>();

        foreach (ItemData item in tousLesObjets)
        {
            string key = GetItemSellKey(item);

            if (!itemsTraites.ContainsKey(key))
            {
                itemsTraites.Add(key, item);
                VerifItemData(item, VendreUnitaire);
            }
        }

        if (VerifIfEmpty())
        {
            EndCommerce();
        }
    }

    private void AddIfVendable(List<ItemData> list, ItemData item)
    {
        if (item != null && item.isVendable)
            list.Add(item);
    }

    private void VerifItemData(ItemData item, Action<ItemData> methode)
    {
        if (item == null || item.prix <= 0) return;

        string key = GetItemSellKey(item);
        GameObject produitItem = Instantiate(produitItemPrefab, parentsProduits.transform);

        if (produitItem.TryGetComponent<UIProduitMarchand>(out var slot))
        {
            slot.SetupPNJAcheteur(item, this);
            slot.nameItem.text = item.itemName;
            slot.iconeItem.sprite = item.visual;

            // --- CALCUL DU STOCK D'OBJETS DE MÊME CLÉ/NIVEAU ---
            int currentStock = GetSellStock(item);

            // Armes équipées dans la palette
            foreach (var weaponSlot in PaletteSystem.instance.slotManager.weapons)
            {
                if (weaponSlot.itemData != null && GetItemSellKey(weaponSlot.itemData) == key)
                    currentStock++;
            }

            // Objets rapides équipés
            foreach (var objectSlot in PaletteSystem.instance.slotManager.objects)
            {
                if (objectSlot.itemData != null && GetItemSellKey(objectSlot.itemData) == key)
                    currentStock++;
            }

            // Armures équipées
            if (GetItemSellKey(EquipmentSystem.instance.headSlot.item) == key) currentStock++;
            if (GetItemSellKey(EquipmentSystem.instance.chestSlot.item) == key) currentStock++;
            if (GetItemSellKey(EquipmentSystem.instance.handsSlot.item) == key) currentStock++;
            if (GetItemSellKey(EquipmentSystem.instance.legsSlot.item) == key) currentStock++;
            if (GetItemSellKey(EquipmentSystem.instance.feetSlot.item) == key) currentStock++;

            // Flèches
            if (GetItemSellKey(EquipmentSystem.instance.arrowItemInInventory.itemData) == key) currentStock++;

            if (slot.stockItemInInventory != null)
                slot.stockItemInInventory.text = $"Stock : {currentStock}";

            // Calcul du prix
            int prixUnitaireRachat = Mathf.RoundToInt(item.prix * pourcentageDeRachat);
            slot.priceItem.text = prixUnitaireRachat.ToString();

            slot.buyButton.onClick.RemoveAllListeners();
            slot.buyButton.onClick.AddListener(() => methode(item));

            if (slot.fillStockButton != null)
            {
                if (currentStock > 1)
                {
                    slot.fillStockButton.gameObject.SetActive(true);
                    int prixTotalRachat = prixUnitaireRachat * currentStock;
                    slot.priceFillStock.text = prixTotalRachat.ToString();

                    slot.fillStockButton.onClick.RemoveAllListeners();
                    slot.fillStockButton.onClick.AddListener(() => VendreTout(item));
                }
                else
                {
                    slot.fillStockButton.gameObject.SetActive(false);
                }
            }

            slot.actionButtonsGroup.SetActive(false);
        }
    }

    private string GetItemSellKey(ItemData item)
    {
        if (item == null) return "";

        if (item.stackable)
        {
            return item.itemID;
        }

        // Identifiant unique combinant l'ID de l'objet et son Niveau d'Amélioration
        return item.itemID + "_LEVEL_" + item.levelAmelioration;
    }

    private void VendreUnitaire(ItemData produit)
    {
        if (produit == null) return;

        int gain = Mathf.RoundToInt(produit.prix * pourcentageDeRachat);
        PlayerController.Instance.Wallet.AddGold(gain);

        // Supprime 1 instance (dans l'inventaire ou équipé)
        SupprimerUneInstance(produit);

        RefreshProduits();
    }

    private void VendreTout(ItemData produit)
    {
        int quantite = GetSellStock(produit);

        // Compter aussi les équipés si nécessaire
        string key = GetItemSellKey(produit);
        foreach (var weaponSlot in PaletteSystem.instance.slotManager.weapons)
            if (weaponSlot.itemData != null && GetItemSellKey(weaponSlot.itemData) == key) quantite++;
        foreach (var objectSlot in PaletteSystem.instance.slotManager.objects)
            if (objectSlot.itemData != null && GetItemSellKey(objectSlot.itemData) == key) quantite++;
        if (GetItemSellKey(EquipmentSystem.instance.headSlot.item) == key) quantite++;
        if (GetItemSellKey(EquipmentSystem.instance.chestSlot.item) == key) quantite++;
        if (GetItemSellKey(EquipmentSystem.instance.handsSlot.item) == key) quantite++;
        if (GetItemSellKey(EquipmentSystem.instance.legsSlot.item) == key) quantite++;
        if (GetItemSellKey(EquipmentSystem.instance.feetSlot.item) == key) quantite++;

        if (quantite <= 0) return;

        int gain = Mathf.RoundToInt(produit.prix * pourcentageDeRachat * quantite);
        PlayerController.Instance.Wallet.AddGold(gain);

        for (int i = 0; i < quantite; i++)
        {
            SupprimerUneInstance(produit);
        }

        RefreshProduits();
    }

    private void SupprimerUneInstance(ItemData produit)
    {
        string targetKey = GetItemSellKey(produit);

        // 1. Chercher dans l'inventaire
        foreach (ItemInInventory item in InventorySystem.instance.GetContent())
        {
            if (item.itemData != null && GetItemSellKey(item.itemData) == targetKey)
            {
                InventorySystem.instance.RemoveItem(item.itemData);
                return;
            }
        }

        // 2. Chercher dans les armes équipées
        for (int i = 0; i < PaletteSystem.instance.slotManager.weapons.Length; i++)
        {
            var weapon = PaletteSystem.instance.slotManager.weapons[i];
            if (weapon.itemData != null && GetItemSellKey(weapon.itemData) == targetKey)
            {
                PaletteSystem.instance.equipmentManager.DesequipWeapon(i + 1);
                return;
            }
        }

        // 3. Chercher dans les objets rapides
        for (int i = 0; i < PaletteSystem.instance.slotManager.objects.Length; i++)
        {
            var obj = PaletteSystem.instance.slotManager.objects[i];
            if (obj.itemData != null && GetItemSellKey(obj.itemData) == targetKey)
            {
                PaletteSystem.instance.equipmentManager.RemoveObject(i + 1);
                return;
            }
        }

        // 4. Chercher dans les pièces d'armure
        if (GetItemSellKey(EquipmentSystem.instance.headSlot.item) == targetKey)
            EquipmentSystem.instance.DesequipEquipment(EquipmentType.Head);
        else if (GetItemSellKey(EquipmentSystem.instance.chestSlot.item) == targetKey)
            EquipmentSystem.instance.DesequipEquipment(EquipmentType.Chest);
        else if (GetItemSellKey(EquipmentSystem.instance.handsSlot.item) == targetKey)
            EquipmentSystem.instance.DesequipEquipment(EquipmentType.Hands);
        else if (GetItemSellKey(EquipmentSystem.instance.legsSlot.item) == targetKey)
            EquipmentSystem.instance.DesequipEquipment(EquipmentType.Legs);
        else if (GetItemSellKey(EquipmentSystem.instance.feetSlot.item) == targetKey)
            EquipmentSystem.instance.DesequipEquipment(EquipmentType.Feet);
    }

    private int GetSellStock(ItemData item)
    {
        int count = 0;
        string key = GetItemSellKey(item);

        foreach (ItemInInventory inv in InventorySystem.instance.GetContent())
        {
            if (inv.itemData != null && GetItemSellKey(inv.itemData) == key)
            {
                count += inv.count;
            }
        }

        return count;
    }

    private bool VerifIfEmpty()
    {
        return PaletteSystem.instance.slotManager.weapons[0].itemData == null &&
               PaletteSystem.instance.slotManager.weapons[1].itemData == null &&
               EquipmentSystem.instance.headSlot.item == null &&
               EquipmentSystem.instance.chestSlot.item == null &&
               EquipmentSystem.instance.handsSlot.item == null &&
               EquipmentSystem.instance.legsSlot.item == null &&
               EquipmentSystem.instance.feetSlot.item == null &&
               InventorySystem.instance.GetContent().Count == 0;
    }

    public void SetCurrentHoveredItem(UIProduitMarchand slot)
    {
        currentSlotProduit = slot;
    }

    private void UpdateGoldPlayerText()
    {
        goldPlayer.text = player.Wallet.GetGoldAmount().ToString();
    }
}