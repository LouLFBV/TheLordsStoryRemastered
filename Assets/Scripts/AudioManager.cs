using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 🟢 Indispensable pour SceneManager et Scene

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource explorationSource;
    [SerializeField] private AudioSource chaseSource;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    // Scènes où le son d'exploration doit être totalement coupé (volume = 0)
    private readonly HashSet<string> bossScenes = new HashSet<string>
    {
        "Boss1",
        "Boss2",
        "Boss3",
        "BossFinal",
        "GrotteSecreteBoss"
    };

    // Volumes max par défaut pour la scène active
    private float _sceneMaxExploVolume = 0.5f;
    private float _sceneMaxChaseVolume = 0.5f;

    private Coroutine _fadeCoroutine;
    private bool _isChased = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Décommente cette ligne si ton AudioManager doit être conservé entre les scènes :
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    #region Gestion des Événements de Scène
    private void OnEnable()
    {
        // 🟢 S'abonne à l'événement de chargement de scène
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 🟢 Toujours se désabonner pour éviter les fuites de mémoire
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Exécuté automatiquement par Unity dès qu'une nouvelle scène est chargée.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // On réinitialise l'état de poursuite au chargement d'une nouvelle scène
        _isChased = false;

        // Calcule la cible du volume d'exploration pour la scène qui vient de charger
        float targetExplo = GetTargetExploVolume(scene.name);
        float targetChase = 0f;

        // Effectue un fondu fluide vers le bon volume
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeMusicSequence(targetExplo, targetChase));
    }
    #endregion

    /// <summary>
    /// Vérifie si un nom de scène correspond à une scène de boss.
    /// </summary>
    private bool IsBossScene(string sceneName)
    {
        return bossScenes.Contains(sceneName);
    }

    /// <summary>
    /// Calcule le volume cible pour l'exploration.
    /// </summary>
    private float GetTargetExploVolume(string sceneName = null)
    {
        string currentScene = sceneName ?? SceneManager.GetActiveScene().name;

        // Si on est en poursuite OU dans une scène de boss, le volume passe à 0
        if (_isChased || IsBossScene(currentScene))
        {
            return 0f;
        }
        return _sceneMaxExploVolume;
    }

    public void ChangeSceneTracks(AudioClip newExploClip, AudioClip newChaseClip, float exploVolume, float chaseVolume)
    {
        _sceneMaxExploVolume = exploVolume;
        _sceneMaxChaseVolume = chaseVolume;
        _isChased = false;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(TransitionToNewSceneTracks(newExploClip, newChaseClip));
    }

    private IEnumerator TransitionToNewSceneTracks(AudioClip newExplo, AudioClip newChase)
    {
        float timer = 0f;
        float startExploVol = explorationSource.volume;
        float startChaseVol = chaseSource.volume;

        // Phase 1 : Fade Out
        while (timer < fadeDuration / 2f)
        {
            timer += Time.deltaTime;
            float t = timer / (fadeDuration / 2f);
            explorationSource.volume = Mathf.Lerp(startExploVol, 0f, t);
            chaseSource.volume = Mathf.Lerp(startChaseVol, 0f, t);
            yield return null;
        }

        explorationSource.clip = newExplo;
        chaseSource.clip = newChase;

        if (newExplo != null) explorationSource.Play();
        if (newChase != null) chaseSource.Play();

        float targetExplo = GetTargetExploVolume();

        // Phase 2 : Fade In
        timer = 0f;
        while (timer < fadeDuration / 2f)
        {
            timer += Time.deltaTime;
            float t = timer / (fadeDuration / 2f);
            explorationSource.volume = Mathf.Lerp(0f, targetExplo, t);
            chaseSource.volume = 0f;
            yield return null;
        }

        explorationSource.volume = targetExplo;
        chaseSource.volume = 0f;
    }

    public void SetChaseState(bool chaseActive)
    {
        if (_isChased == chaseActive) return;
        _isChased = chaseActive;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        float targetExplo = GetTargetExploVolume();
        float targetChase = _isChased ? _sceneMaxChaseVolume : 0f;

        _fadeCoroutine = StartCoroutine(FadeMusicSequence(targetExplo, targetChase));
    }

    private IEnumerator FadeMusicSequence(float targetExplo, float targetChase)
    {
        float timer = 0f;
        float startExplo = explorationSource.volume;
        float startChase = chaseSource.volume;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            explorationSource.volume = Mathf.Lerp(startExplo, targetExplo, t);
            chaseSource.volume = Mathf.Lerp(startChase, targetChase, t);

            yield return null;
        }

        explorationSource.volume = targetExplo;
        chaseSource.volume = targetChase;
    }
}