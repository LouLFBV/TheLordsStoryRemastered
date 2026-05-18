using UnityEngine;

public class CanHarvest : MonoBehaviour
{
    [SerializeField] private InteractSystem interactBehaviour;

    [SerializeField] private bool isAxe = false;

    [SerializeField] private bool isPickaxe = false;
    [SerializeField] private bool isSuperPickaxe = false;

    private void OnEnable()
    {
        Debug.Log("Enabling CanHarvest: " + gameObject.name);
        if (isAxe)
        {
            interactBehaviour.canAxe = true;
        }
        if (isPickaxe)
        {
            interactBehaviour.canPickaxe = true;
        }
        if (isSuperPickaxe)
        {
            interactBehaviour.canSuperPickaxe = true;
        }
    }
    private void OnDisable()
    {
        if (isAxe)
        {
            interactBehaviour.canAxe = false;
        }
        if (isPickaxe)
        {
            interactBehaviour.canSuperPickaxe = false;
        }
        if (isSuperPickaxe)
        {
            interactBehaviour.canSuperPickaxe = false;
        }
    }
}

