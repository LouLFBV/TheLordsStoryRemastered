using UnityEngine;

public class ButtonBackToMenu : MonoBehaviour
{
    public void BackToMenu()
    {
        Debug.Log("Retour au menu principal");
        // Charger la scène du menu principal
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
