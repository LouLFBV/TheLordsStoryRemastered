using UnityEngine;

public class DialoguePanelEvents : MonoBehaviour
{
    public void CloseDialoguePanel()
    {
        DialogueManager.instance.CloseDialoguePanel();
    }

}
