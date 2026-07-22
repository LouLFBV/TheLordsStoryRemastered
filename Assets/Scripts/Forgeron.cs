using System.Collections.Generic;
using UnityEngine;

public class Forgeron : PNJParent
{
    [Header("Forgeron Panel")]
    [SerializeField] private ForgeronUI forgeronUI ;

    public override void OnInteract(PlayerInteractor player)
    {
        if (forgeronUI.isOpen) return;
        if (isOnDial && Time.time - dialogueStartTime > inputCooldown /*&& !animatorPanelProduits.GetBool("PanelIsOpen")*/)
        {
            if (!DialogueManager.instance.SkipOrFinish(currentSpeaker) && !DialogueManager.instance.inDelay)
                StartDialogue(sentences);
        }
        else
        {
            SetTargeted(false, playerTransform);
            StartDialogue(sentences);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            forgeronUI.player = player;
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
    public void StartDialogue(List<DialogueResponse> sentence = null)
    {
        if (index == 0 && leghthSentences == sentences.Count)
        {
            if (VerifIfEmpty())
            {
                sentence.Add(
                new DialogueResponse
                {
                    pnjDialogues = new string[] { "Hmm je vois que tu n'as pas d'armes. Reviens me voir lorsque tu en auras !" },
                    playerResponses = new string[] { "Pas de soucis, à bientôt !" }
                }
                );
            }
            else
            {
                sentence.Add(
                new DialogueResponse
                {
                    pnjDialogues = new string[] { "Montre moi ce que tu as !" },
                    playerResponses = new string[] {}
                }
                );
            }
        }

        if (!isOnDial)
        {
            StartCoroutine(RotateTowardsPlayer());

            player.RequestedPanelType = UIPanelType.Dialogue;
            player.StateMachine.ChangeState(PlayerStateType.UI);
            isOnDial = true;
            animator.SetInteger("talkIndex", Random.Range(0, 3));
            animator.SetBool("isTalking", true);


            DialogueManager.instance.ActiveDesactiveDialoguePanel(DialogueManager.instance.animatorDialoguePanel);

            index = 0;
            dialogueStartTime = Time.time; // Enregistrer le temps de début du dialogue
            currentDialogue = sentence;
        }
        if (index >= currentDialogue.Count /*&& !animatorPanelProduits.GetBool("PanelIsOpen")*/)
        {
            if (!VerifIfEmpty())
            {
                Debug.Log("Opening Forgeron UI");
                forgeronUI.OpenForgeonUI();
            }
            EndDiscussion(!VerifIfEmpty());
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

    private bool VerifIfEmpty()
    {
        return PaletteSystem.instance.slotManager.weapons[0].itemData == null && PaletteSystem.instance.slotManager.weapons[1].itemData == null &&
                EquipmentSystem.instance.headSlot.item == null && EquipmentSystem.instance.chestSlot.item == null &&
                EquipmentSystem.instance.handsSlot.item == null && EquipmentSystem.instance.legsSlot.item == null &&
                EquipmentSystem.instance.feetSlot.item == null && InventorySystem.instance.GetContent().Count == 0;
    }
}
