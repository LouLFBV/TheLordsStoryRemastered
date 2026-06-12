using UnityEngine;

public class ButtonBackToMenu : MonoBehaviour
{
    public void BackToMenu()
    {
        // Charger la scène du menu principal
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
