using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI")]
    public TextMeshProUGUI textName;
    public TextMeshProUGUI textNickname;
    public TextMeshProUGUI textDialogue;
    public GameObject dialoguePanel;
    public Animator animatorDialoguePanel;
    public GameObject playerIcone;
    public GameObject questButtons; // panneau avec les boutons Accepter/Refuser


    [Header("Others")]
    [SerializeField] private GameObject iconeToSkip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float typingSpeed = 0.03f;
    public bool inDelay;
    public enum Speaker { PNJ, Player}
    private Coroutine typingCoroutine;
    private bool isTyping;
    private string currentText;
    [SerializeField] private GameObject forUIManager;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        animatorDialoguePanel = dialoguePanel.GetComponent<Animator>();
    }

    public void ShowQuestButtons(PNJ pnj)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        questButtons.SetActive(true);
        iconeToSkip.SetActive(false);

        // Brancher boutons sur le PNJ actif
        Button[] buttons = questButtons.GetComponentsInChildren<Button>();
        buttons[0].onClick.RemoveAllListeners();
        buttons[0].onClick.AddListener(() => pnj.AcceptQuest());

        buttons[1].onClick.RemoveAllListeners();
        buttons[1].onClick.AddListener(() => pnj.RefuseQuest());
    }

    public void HideQuestButtons()
    {
        iconeToSkip.SetActive(true);
        questButtons.SetActive(false);
    }


    public void ShowLine(string line, Speaker speaker = Speaker.PNJ, float delay = 0f)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        //if (speaker == Speaker.PNJ)
        //    typingCoroutine = StartCoroutine(TypeTextWithDelay(line, typingSpeed, delay, textDialogue));
        //else
        //    typingCoroutine = StartCoroutine(TypeTextWithDelay(line, typingSpeed, delay, textPlayerDialogue));
        typingCoroutine = StartCoroutine(TypeTextWithDelay(line, typingSpeed, delay, textDialogue));
    }

    //private IEnumerator TypeTextWithDelay(string line, float typingSpeed, float delay, TextMeshProUGUI textField)
    //{
    //    inDelay = true;
    //    if (delay > 0f)
    //        yield return new WaitForSeconds(delay);
    //    inDelay = false;

    //    isTyping = true;
    //    currentText = line;
    //    textField.text = "";

    //    foreach (char c in line)
    //    {
    //        textField.text += c;
    //        if(audioSource.clip != null)
    //            audioSource.PlayOneShot(audioSource.clip);
    //        yield return new WaitForSeconds(typingSpeed);
    //    }

    //    isTyping = false;
    //}

    private IEnumerator TypeTextWithDelay(string line, float typingSpeed, float delay, TextMeshProUGUI textField)
    {
        inDelay = true;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        inDelay = false;

        isTyping = true;
        currentText = line;
        textField.text = "";

        int charIndex = 0;

        foreach (char c in line)
        {
            textField.text += c;

            // Joue le son une fois sur deux caractères
            if (charIndex % 2 == 0 && audioSource.clip != null)
            {
                audioSource.PlayOneShot(audioSource.clip);
            }

            charIndex++;

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }


    public bool SkipOrFinish(Speaker speaker)
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            //if (speaker == Speaker.PNJ)
            //    textDialogue.text = currentText;
            //else
            //    textPlayerDialogue.text = currentText;
            textDialogue.text = currentText;
            isTyping = false;
            return true; 
        }
        return false; 
    }

    public void ActiveDesactiveDialoguePanel(Animator animator)
    {
        Debug.Log("Toggling dialogue panel. Current state: " + animator.GetBool("PanelIsOpen"));
        bool isOpen = animator.GetBool("PanelIsOpen");
        animator.SetBool("PanelIsOpen", !isOpen);
        forUIManager.SetActive(animatorDialoguePanel.GetBool("PanelIsOpen"));
    }

    public void CloseDialoguePanel()
    {
        textDialogue.text = "";
        textName.text = "";
    }

    public void SetSpeakerName(Speaker speaker, string name, string nickName = "")
    {
        textName.text = name;
        if (!string.IsNullOrEmpty(nickName))
            textNickname.text = " - " + nickName;
        else
            textNickname.text = "";
        playerIcone.SetActive(speaker == Speaker.Player);
    }
}
