using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PNJParent : InteractableBase
{
    [Header("Panel")]
    [SerializeField] protected GameObject parentsProduits;
    [SerializeField] protected Animator animatorPanelProduits;
    [SerializeField] private List<ItemData> produits;
    [SerializeField] protected GameObject produitItemPrefab;
    [SerializeField] protected GameObject isActive;

    [Header("PNJ")]
    public string namePNJ;
    public string nicknamePNJ;
    public List<DialogueResponse> sentences;
    public bool isOnDial;
    protected int index = 0;
    protected Transform playerTransform;
    protected PlayerController player;
    protected bool isPlayerInZone;
    protected Animator animator;
    private float vitesseDeRotation = 0.15f;
    protected int sentenceIndex = 0;
    protected List<DialogueResponse> currentDialogue; // tableau actif
    protected DialogueManager.Speaker currentSpeaker;

    protected int leghthSentences;
    [HideInInspector] public float inputCooldown = 1f; // Temps d'attente après lancement du dialogue
    protected float dialogueStartTime;
    [HideInInspector] public float dialogueEndTime;
    
    private void Start()
    {
        leghthSentences = sentences.Count;
        animator = GetComponent<Animator>();
        playerTransform = PlayerController.Instance.transform;
    }

    public override void OnInteract(PlayerInteractor player){}

    public void EndCommerce()
    {
        Debug.Log("[PNJParent] EndCommerce()");
        isOnDial = false;
        player.StateMachine.ChangeState(PlayerStateType.Idle);
        dialogueEndTime = Time.time;
        animatorPanelProduits.SetBool("PanelIsOpen", false);
        isActive.SetActive(false);
        animator.SetBool("isTalking", false);
    }
    public void EndDiscussion(bool haveItems = true)
    {
        if (!haveItems)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
            Debug.Log("[PNJParent] EndDiscussion() - No items, just ending dialogue");
            isOnDial = false;
        }
        animator.SetBool("isTalking", false);
        dialogueEndTime = Time.time;
        index = 0;
        sentences.RemoveAt(sentences.Count - 1);

        if (DialogueManager.instance.dialoguePanel.transform.localScale.y > 0)
            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);

    }
    protected IEnumerator RotateTowardsPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            float t = 0f;
            while (t < 1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, t);
                t += Time.deltaTime * vitesseDeRotation; // vitesse de rotation
                yield return null; // attendre la frame suivante
            }
        }
    }

    public void OpenProduitsPanel()
    {
        animatorPanelProduits.SetBool("PanelIsOpen", true);
        isActive.SetActive(true);
    }
}
