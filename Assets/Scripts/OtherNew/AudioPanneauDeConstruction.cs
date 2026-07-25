using UnityEngine;

public class AudioPanneauDeConstruction : MonoBehaviour
{
    public static AudioPanneauDeConstruction Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            // Sécurité : si aucun AudioSource n'est présent, on en ajoute un
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    /// <summary>
    /// Joue un clip audio UI/Craft de manière 2D (non affecté par la position 3D dans le monde).
    /// </summary>
    public void PlayCraftSound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}