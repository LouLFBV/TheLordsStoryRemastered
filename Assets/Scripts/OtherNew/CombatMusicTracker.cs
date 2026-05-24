using System.Collections.Generic;
using UnityEngine;

public class CombatMusicTracker : MonoBehaviour
{
    public static CombatMusicTracker Instance { get; private set; }

    // Utilise un HashSet pour éviter qu'un même ennemi ne s'ajoute deux fois par erreur
    private HashSet<Collider> _activeChasers = new HashSet<Collider>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Survit aux changements de scènes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        _activeChasers.Clear();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetChaseState(false);
        }
    }

    public void RegisterChaser(Collider enemyCollider)
    {
        if (_activeChasers.Add(enemyCollider))
        {
            UpdateMusicState();
        }
    }

    public void UnregisterChaser(Collider enemyCollider)
    {
        if (_activeChasers.Remove(enemyCollider))
        {
            UpdateMusicState();
        }
    }

    private void UpdateMusicState()
    {
        if (AudioManager.Instance == null) return;

        // Si la liste contient au moins 1 ennemi, on active la musique de chasse
        bool shouldPlayChaseMusic = _activeChasers.Count > 0;
        AudioManager.Instance.SetChaseState(shouldPlayChaseMusic);
    }
}