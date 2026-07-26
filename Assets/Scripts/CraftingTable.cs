using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class CraftingTable : InteractableBase
{
    [SerializeField] private GameObject craftPanel;

    [Header("Global Database Reference")]
    [SerializeField] private AllRecipeData allRecipeData;

    [Header("Recipes Data")]
    [SerializeField] private List<ItemData> potionsRecipe;
    [SerializeField] private List<ItemData> weaponsRecipe;
    [SerializeField] private List<ItemData> equipmentsRecipe;

    [Header("Containers & Prefabs")]
    [SerializeField] private GameObject slotRecipePrefab;
    [SerializeField] private Transform recipeSlotContainer; // Le container pour ranger les slots de recettes !
    [SerializeField] private Button equipmentButton, weaponButton, potionButton;

    [Header("Description Panel")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image itemIcone; // Changé de Sprite à Image pour le Canvas UI
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
        if (craftPanel != null && animatorPanelProduits.GetBool("PanelIsOpen"))
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
        if (craftPanel != null && !animatorPanelProduits.GetBool("PanelIsOpen"))
        {
            craftPanel.SetActive(true);

            if (animatorPanelProduits != null)
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
                // Sécurité au cas où le bouton n'est pas branché dans l'inspecteur
                RefreshDisplay(CraftingType.Weapons);
            }
        }
    }

    public void ClosePanel()
    {
        if (craftPanel != null) craftPanel.SetActive(false);

        // Sécurité UI : On cache le panneau de description pour la prochaine ouverture
        //if (descriptionPanel != null) descriptionPanel.SetActive(false);
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
        // 1. Nettoyage de l'UI
        foreach (Transform child in recipeSlotContainer)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        descriptionPanel.SetActive(false);
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
                    // On n'affiche que si le résultat est un consommable
                    if (item.itemType != ItemType.Consumable) continue;
                    break;

                case CraftingType.Weapons:
                    // On n'affiche que si c'est un équipement de type Arme
                    if (item.itemType != ItemType.Equipment || item.equipmentType != EquipmentType.Weapon) continue;
                    break;

                case CraftingType.Equipments:
                    // On n'affiche que si c'est une armure/bouclier (Équipement, mais PAS une arme)
                    if (item.itemType != ItemType.Equipment || item.equipmentType == EquipmentType.Weapon) continue;
                    break;
            }

            // 4. Si la recette a passé les filtres ci-dessus, on crée son bouton dans l'UI
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
        // Nettoyage de la zone des ingrédients requis
        foreach (Transform child in ingredientContainer)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // On vérifie si l'item possède bien une recette (RecipeData) assignée
        if (targetItem.recipe == null || targetItem.recipe.ingredients == null || targetItem.recipe.ingredients.Count == 0)
        {
            Debug.LogWarning($"[CRAFT] {targetItem.itemName} n'a pas de RecipeData assigné !");
            if (craftButton != null) craftButton.interactable = false;
            return;
        }

        bool canCraftAll = true;

        foreach (Transform child in ingredientContainer)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        foreach (var ingredient in targetItem.recipe.ingredients)
        {
            if (ingredient.itemNeeded == null) continue;

            

            GameObject ingredUI = Instantiate(ingredientSlotPrefab, ingredientContainer);

            if (ingredUI.TryGetComponent<SlotRecette>(out var slotScript))
            {
                int currentStock = InventorySystem.instance.GetItemCount(ingredient.itemNeeded);

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

        // 1. On consomme les ingrédients requis (💡 Utilisation de var ici aussi)
        foreach (var ingredient in _currentSelectedTargetItem.recipe.ingredients)
        {
            for (int i = 0; i < ingredient.amountNeededItem; i++)
            {
                InventorySystem.instance.RemoveItem(ingredient.itemNeeded);
            }
        }

        // 2. On donne l'objet crafté au joueur
        InventorySystem.instance.AddItem(_currentSelectedTargetItem, _currentSelectedTargetItem.recipe.craftableAmount);
        Debug.Log($"<color=green>[CRAFT] Réussite ! +1 {_currentSelectedTargetItem.itemName} ajouté à l'inventaire.</color>");

        // 3. On actualise l'UI globale et les stocks restants pour voir si on peut en fabriquer un deuxième
        InventorySystem.instance.RefreshContent();
        if (audioSource != null && craftSound != null) audioSource.PlayOneShot(craftSound);
        RefreshRequiredIngredients(_currentSelectedTargetItem);
    }
}
public enum CraftingType
{
    Potions,
    Weapons,
    Equipments
}
