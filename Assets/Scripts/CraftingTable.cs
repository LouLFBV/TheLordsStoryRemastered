using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class CraftingTable : InteractableBase
{
    [SerializeField] private GameObject craftPanel;

    [Header("Global Database Reference")]
    [SerializeField] private AllRecipeData allRecipeData;

    [Header("Containers & Prefabs")]
    [SerializeField] private GameObject slotRecipePrefab;
    [SerializeField] private Transform recipeSlotContainer; // Container pour ranger les slots de recettes !
    [SerializeField] private Button equipmentButton, weaponButton, potionButton;

    [Header("Description Panel")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image itemIcone;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Button craftButton;

    [Header("Animation Panel")]
    [SerializeField] private Animator animatorPanelProduits;

    [Header("Ingredients Panel")]
    [SerializeField] private GameObject ingredientSlotPrefab;
    [SerializeField] private Transform ingredientContainer;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip craftSound;

    private ItemData _currentSelectedTargetItem;

    private void Start()
    {
        // On lie les boutons de catégories aux listes correspondantes
        if (equipmentButton != null) equipmentButton.onClick.AddListener(() => RefreshDisplay(CraftingType.Equipments));
        if (weaponButton != null) weaponButton.onClick.AddListener(() => RefreshDisplay(CraftingType.Weapons));
        if (potionButton != null) potionButton.onClick.AddListener(() => RefreshDisplay(CraftingType.Potions));

        // On cache la description au départ tant qu'aucune recette n'est sélectionnée
        if (descriptionPanel != null) descriptionPanel.SetActive(false);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Détection de la touche fermeture (Échap / Manette)
        if (craftPanel != null && animatorPanelProduits != null && animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            if (PlayerController.Instance != null && PlayerController.Instance.Input != null)
            {
                if (PlayerController.Instance.Input.CloseMenuPressed || PlayerController.Instance.Input.MenuPressed)
                {
                    ClosePanel();
                    PlayerController.Instance.Input.UseCloseMenuInput();
                }
            }
        }
    }

    public override void OnInteract(PlayerInteractor player)
    {
        if (craftPanel != null && animatorPanelProduits != null && !animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            craftPanel.SetActive(true);
            animatorPanelProduits.SetBool("PanelIsOpen", true);

            if (PlayerController.Instance != null && PlayerController.Instance.StateMachine != null)
            {
                PlayerController.Instance.RequestedPanelType = UIPanelType.Dialogue;
                PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);
                SetTargeted(false, PlayerController.Instance.transform);
            }

            if (weaponButton != null)
            {
                weaponButton.onClick.Invoke();
                weaponButton.Select();
            }
            else
            {
                RefreshDisplay(CraftingType.Weapons);
            }
        }
    }

    public void ClosePanel()
    {
        if (descriptionPanel != null) descriptionPanel.SetActive(false);
        _currentSelectedTargetItem = null;

        if (animatorPanelProduits != null)
            animatorPanelProduits.SetBool("PanelIsOpen", false);

        if (PlayerController.Instance != null && PlayerController.Instance.StateMachine != null)
        {
            PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    private void RefreshDisplay(CraftingType craftingType)
    {
        // 1. Nettoyage PROPRE de l'UI
        ClearContainer(recipeSlotContainer);

        if (descriptionPanel != null) descriptionPanel.SetActive(false);
        _currentSelectedTargetItem = null;

        if (allRecipeData == null)
        {
            Debug.LogError("[CRAFT] AllRecipeData n'est pas assigné dans la CraftingTable !");
            return;
        }

        // 2. On parcourt l'UNIQUE liste globale de recettes débloquées
        foreach (RecipeData recipe in allRecipeData.unlockedRecipes)
        {
            if (recipe == null || recipe.craftableItem == null) continue;

            ItemData item = recipe.craftableItem;

            // 3. DISTINCTION DYNAMIQUE SELON LE TYPE DU RÉSULTAT
            switch (craftingType)
            {
                case CraftingType.Potions:
                    if (item.itemType != ItemType.Consumable) continue;
                    break;

                case CraftingType.Weapons:
                    if (item.itemType != ItemType.Equipment || item.equipmentType != EquipmentType.Weapon) continue;
                    break;

                case CraftingType.Equipments:
                    if (item.itemType != ItemType.Equipment || item.equipmentType == EquipmentType.Weapon) continue;
                    break;
            }

            // 4. Si la recette a passé les filtres, on crée son bouton dans l'UI
            GameObject newSlot = Instantiate(slotRecipePrefab, recipeSlotContainer);
            if (newSlot.TryGetComponent<SlotRecipe>(out var slotScript))
            {
                slotScript.Initialize(item, OnRecipeSelected);
            }
        }
    }

    // Appelée automatiquement quand le joueur clique sur un des slots de recette générés
    private void OnRecipeSelected(ItemData selectedItem)
    {
        _currentSelectedTargetItem = selectedItem;

        // Mise à jour des textes et visuels globaux de l'item ciblé
        if (itemName != null) itemName.text = selectedItem.itemName;
        if (itemIcone != null) itemIcone.sprite = selectedItem.visual;
        if (itemDescription != null) itemDescription.text = selectedItem.description;

        // Logique de rafraîchissement des ingrédients requis
        RefreshRequiredIngredients(selectedItem);
        if (descriptionPanel != null) descriptionPanel.SetActive(true);
    }

    private void RefreshRequiredIngredients(ItemData targetItem)
    {
        // Nettoyage propre du conteneur d'ingrédients
        ClearContainer(ingredientContainer);

        if (targetItem == null) return;

        // Récupération de la recette : Soit sur l'ItemData, soit recherche fallback dans AllRecipeData
        RecipeData recipe = targetItem.recipe;
        if (recipe == null && allRecipeData != null)
        {
            recipe = allRecipeData.unlockedRecipes.Find(r => r != null && r.craftableItem == targetItem);
        }

        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
        {
            Debug.LogWarning($"[CRAFT] {targetItem.itemName} n'a pas de RecipeData valide !");
            if (craftButton != null) craftButton.interactable = false;
            return;
        }

        bool canCraftAll = true;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.itemNeeded == null) continue;

            GameObject ingredUI = Instantiate(ingredientSlotPrefab, ingredientContainer);

            if (ingredUI.TryGetComponent<SlotRecette>(out var slotScript))
            {
                int currentStock = InventorySystem.instance != null ? InventorySystem.instance.GetItemCount(ingredient.itemNeeded) : 0;

                // Initialisation visuelle (Vert / Rouge)
                bool hasEnough = slotScript.SetupIngredient(ingredient.itemNeeded, ingredient.amountNeededItem, currentStock);

                if (!hasEnough) canCraftAll = false;
            }
        }

        if (craftButton != null) craftButton.interactable = canCraftAll;
    }

    // À lier sur le bouton "Fabriquer" (craftButton) dans l'inspecteur Unity
    public void OnCraftButtonPressed()
    {
        if (_currentSelectedTargetItem == null || craftButton == null || !craftButton.interactable) return;

        RecipeData recipe = _currentSelectedTargetItem.recipe;
        if (recipe == null && allRecipeData != null)
        {
            recipe = allRecipeData.unlockedRecipes.Find(r => r != null && r.craftableItem == _currentSelectedTargetItem);
        }

        if (recipe == null || recipe.ingredients == null) return;

        // 1. Consommation des ingrédients requis
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.itemNeeded == null) continue;

            for (int i = 0; i < ingredient.amountNeededItem; i++)
            {
                InventorySystem.instance.RemoveItem(ingredient.itemNeeded);
            }
        }

        // 2. On donne l'objet crafté au joueur
        int amountToCraft = recipe.craftableAmount > 0 ? recipe.craftableAmount : 1;
        InventorySystem.instance.AddItem(_currentSelectedTargetItem, amountToCraft);
        Debug.Log($"<color=green>[CRAFT] Réussite ! +{amountToCraft} {_currentSelectedTargetItem.itemName} ajouté à l'inventaire.</color>");

        // 3. Actualisation de l'UI et du son
        if (InventorySystem.instance != null) InventorySystem.instance.RefreshContent();
        if (audioSource != null && craftSound != null) audioSource.PlayOneShot(craftSound);
        RefreshRequiredIngredients(_currentSelectedTargetItem);
    }

    /// <summary>
    /// Méthode utilitaire inversée pour vider un conteneur UI sans détruire l'itération de la boucle.
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}

public enum CraftingType
{
    Potions,
    Weapons,
    Equipments
}