using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PNJ : InteractableBase
{
    #region Champs/Paramètres
    [Header("Dialogue")]
    public string namePNJ;
    public string nicknamePNJ;
    public DialogueResponse[] sentences; // Dialogue par défaut
    public bool isOnDial;
    private int index = 0;
    private int sentenceIndex = 0;
    private DialogueResponse[] currentDialogue; // tableau actif
    private DialogueManager.Speaker currentSpeakerDisplaying;


    [Header("Quêtes")]
    public QuestSO[] questsDisponibles;
    private int currentQuestIndex = 0;
    private QuestSO currentQuestSO;
    [SerializeField] private QuestInstance activeQuestInstance;
    public bool canGiveQuest;
    private bool isPnjInteraction; 


    [Header("Wandering")]
    [SerializeField] private bool canWander = true;
    [SerializeField] private Transform wanderCenter;
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float wanderDelay = 3f;
    private bool canWanderOnStart;
    private float wanderTimer;
    private Vector3 targetPosition;

    private Transform playerTransform; 
    private PlayerController player; 
    private bool isPlayerInZone;

    private Animator animator;
    private NavMeshAgent agent;
    private float vitesseDeRotation = 0.15f;

    public float dialogueEndTime;
    public float inputCooldown = 1f;
    private float dialogueStartTime;
    private float inputCooldownEnding = 2f;
    #endregion

    private void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        animator.SetBool("isTalking", false);
        wanderTimer = 0f;

        if (wanderCenter == null) wanderCenter = transform;
        canWanderOnStart = canWander;
    }


    public override void OnInteract(PlayerInteractor player)
    {
        Debug.Log("Interacting with PNJ: " + namePNJ);
        if (isOnDial && Time.time - dialogueStartTime > inputCooldown)
        {
            if (!DialogueManager.instance.SkipOrFinish(currentSpeakerDisplaying) && !DialogueManager.instance.inDelay)
                NextLine();
        }
        else if (!isOnDial && Time.time - dialogueEndTime > inputCooldownEnding)
        {
            StartDialogue();
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
        if (isPlayerInZone)
        {
            agent.ResetPath();
            animator.SetFloat("Speed", 0f);
        }

        if (canWander && !isOnDial && !isPlayerInZone)
            Wander();

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void Wander()
    {
        wanderTimer -= Time.deltaTime;

        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            if (wanderTimer <= 0f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += wanderCenter.position;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    targetPosition = hit.position;
                    agent.SetDestination(targetPosition);
                }

                wanderTimer = wanderDelay;
            }
        }
    }
    #region Start/End Dialogue
    public void StartDialogue()
    {
        if (canGiveQuest)
            ResolveQuestInstance();

        if (player == null)
        {
            Debug.LogError("Player reference is null in PNJ. Cannot start dialogue.");
            return;
        }
        player.RequestedPanelType = UIPanelType.Dialogue;
        player.StateMachine.ChangeState(PlayerStateType.UI);

        // 2. Logique propre au PNJ
        animator.SetFloat("Speed", 0f);
        canWander = false;
        isOnDial = true;
        agent.isStopped = true;

        StartCoroutine(RotateTowardsToPlayer());

        DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);
        index = 0;
        dialogueStartTime = Time.time;

        VerifIfIntercationQuest();
        VerifObjectsInInventory();
        AddEnemiesKilled();


        if (canGiveQuest && currentQuestSO != null)
        {
            if (activeQuestInstance == null)
            {
                // Nouvelle quête
                currentDialogue = currentQuestSO.sentencesBeforeQuest;
            }
            else
            {
                switch (activeQuestInstance.status)
                {
                    case QuestStatus.InProgress:
                        if (NewQuestManager.instance.CanCompleteQuest(activeQuestInstance))
                            CompleteQuest();
                        else
                            currentDialogue = currentQuestSO.sentencesQuestInProgress;
                        break;

                    case QuestStatus.Completed:
                        currentDialogue = currentQuestSO.sentencesQuestCompleted;
                        break;
                }
            }
        }
        else if (!isPnjInteraction)
        {
            currentDialogue = sentences;
        }

        // Démarre la lecture
        NextLine();
    }

    private void EndDialogue()
    {
        isOnDial = false;
        index = 0;
        sentenceIndex = 0;
        dialogueEndTime = Time.time;

        if (DialogueManager.instance.dialoguePanel.transform.localScale.y > 0f)
            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);

        animator.SetBool("isTalking", false);
        if (canWanderOnStart) canWander = true;
        player.StateMachine.ChangeState(PlayerStateType.Idle);
        agent.isStopped = false;
        if (isPnjInteraction) isPnjInteraction = false;
    }
#endregion
    public void NextLine()
    {
        Debug.Log("NextLine called for PNJ: " + namePNJ);
        if (index >= currentDialogue.Length)
        {
            if (activeQuestInstance != null)
            {
                if (activeQuestInstance.data != null)
                {
                    if (activeQuestInstance.status == QuestStatus.Completed && !activeQuestInstance.rewardsGiven)
                    {
                        NewQuestManager.instance.ApplyRewards(activeQuestInstance);
                        EndDialogue();
                    }
                    else
                        EndDialogue();
                }
                else if (canGiveQuest && currentDialogue == currentQuestSO.sentencesBeforeQuest)
                {
                    DialogueManager.instance.ShowQuestButtons(this);
                    animator.SetBool("isTalking", false);
                }
                else
                    EndDialogue();
            }
            else if (canGiveQuest && currentDialogue == currentQuestSO.sentencesBeforeQuest)
            {
                DialogueManager.instance.ShowQuestButtons(this);
                animator.SetBool("isTalking", false);
            }
            else
                EndDialogue();
            return;
        }

        var dialogueGroup = currentDialogue[index];

        // Affiche le dialogue PNJ ou la réponse du joueur selon l'index
        if (sentenceIndex < dialogueGroup.pnjDialogues.Length)
        {
            currentSpeakerDisplaying = DialogueManager.Speaker.PNJ;

            DialogueManager.instance.SetSpeakerName(DialogueManager.Speaker.PNJ, namePNJ, nicknamePNJ);
            DialogueManager.instance.ShowLine(dialogueGroup.pnjDialogues[sentenceIndex], DialogueManager.Speaker.PNJ);
            animator.SetBool("isTalking", true);
        }
        else
        {
            currentSpeakerDisplaying = DialogueManager.Speaker.Player;
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

    #region Accept/Refuse Quest
    public void AcceptQuest()
    {
        NewQuestManager.instance.AddQuest(currentQuestSO);
        activeQuestInstance = NewQuestManager.instance.GetQuestInstance(currentQuestSO);

        DialogueManager.instance.HideQuestButtons();

        animator.SetBool("isTalking", true);
        index = 0;
        currentDialogue = currentQuestSO.sentencesQuestAccepted;
        NewQuestLog.instance.TrackQuestOnHUD(activeQuestInstance);
        Debug.Log("Quest accepted: " + currentQuestSO.questName);
        NextLine();
    }


    public void RefuseQuest()
    {

        DialogueManager.instance.HideQuestButtons();
        animator.SetBool("isTalking", true);
        index = 0;
        currentDialogue = currentQuestSO.sentencesQuestRefused;
        NextLine();
        activeQuestInstance = null;
    }
    #endregion
    private void CompleteQuest()
    {
        if (activeQuestInstance == null) return;

        NewQuestManager.instance.CompleteQuest(activeQuestInstance);

        if (currentQuestSO.requiredItem != null)
            DeleteObjectsInInventory(currentQuestSO.requiredItem, currentQuestSO.requiredItemCount);

        if (NewQuestLog.instance.currentlyTrackedQuest != null && NewQuestLog.instance.currentlyTrackedQuest.data == currentQuestSO)
        {
            NewQuestLog.instance.UntrackQuest();
        }

        index = 0;
        currentDialogue = currentQuestSO.sentencesQuestCompleted;
        currentQuestIndex++;
        if (currentQuestIndex >= questsDisponibles.Length)
            canGiveQuest = false;
    }


    private void VerifObjectsInInventory()
    {
        if (activeQuestInstance == null || activeQuestInstance.status != QuestStatus.InProgress || currentQuestSO.requiredItem == null) return;

        activeQuestInstance.currentCount = 0;

        foreach (var obj in InventorySystem.instance.GetContent())
        {
            if (currentQuestSO.requiredItem == obj.itemData)
            {
                activeQuestInstance.currentCount += obj.count;
            }
        }

        if (NewQuestLog.instance.currentlyTrackedQuest == activeQuestInstance)
        {
            NewQuestLog.instance.UpdateHUDToggleState();
        }
    }

    private void AddEnemiesKilled()
    {
        if (activeQuestInstance == null || activeQuestInstance.status != QuestStatus.InProgress || currentQuestSO.questType != QuestType.Hunt) return;
        foreach (var quest in NewQuestManager.instance.activeQuests)
        {
            if (quest.data == currentQuestSO)
            {
                activeQuestInstance.currentCount = quest.currentCount;
            }
        }
    }

    private void DeleteObjectsInInventory(ItemData itemData, int count)
    {
        for (int i = 0; i < count; i++)
        {
            InventorySystem.instance.RemoveItem(itemData);
        }
    }
    private void VerifIfIntercationQuest()
    {
        foreach (var quest in NewQuestManager.instance.activeQuests)
        {
            if (quest.data.namePNJ == namePNJ && quest.status == QuestStatus.InProgress)
            {
                quest.interactionDone = true;
                activeQuestInstance = quest;
                currentQuestSO = quest.data;
                currentDialogue = currentQuestSO.sentencesInteraction;
                isPnjInteraction = true;
                return;
            }
        }
    }

    private void ResolveQuestInstance()
    {
        activeQuestInstance = null;
        currentQuestSO = null;

        //  On commence à l'index courant
        for (int i = currentQuestIndex; i < questsDisponibles.Length; i++)
        {
            var questSO = questsDisponibles[i];
            var instance = NewQuestManager.instance.GetQuestInstance(questSO);

            // 1️⃣ Quête jamais acceptée
            if (instance == null  && !NewQuestManager.instance.IsFinished(questSO))
            {
                currentQuestSO = questSO;
                currentQuestIndex = i; //  important
                return;
            }
            if (instance == null) continue;

            // 2️⃣ Quête en cours
            if (instance.status == QuestStatus.InProgress)
            {
                activeQuestInstance = instance;
                currentQuestSO = questSO;
                currentQuestIndex = i;
                return;
            }

            // 3️⃣ Quête terminée mais récompense pas encore donnée
            if (instance.status == QuestStatus.Completed && !instance.rewardsGiven)
            {
                activeQuestInstance = instance;
                currentQuestSO = questSO;
                currentQuestIndex = i;
                return;
            }

            // 4️⃣ Si elle est totalement terminée  on passe à la suivante
        }

        // 5️⃣ Toutes les quêtes sont terminées
        canGiveQuest = false;
    }



    private IEnumerator RotateTowardsToPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            float angle = Quaternion.Angle(transform.rotation, lookRotation);

            while (angle > 1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * (vitesseDeRotation * 10f));
                angle = Quaternion.Angle(transform.rotation, lookRotation);
                yield return null;
            }
        }
    }
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(transform.position, distanceToInteract);

        if (wanderCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wanderCenter.position, wanderRadius);
        }
    }
}
