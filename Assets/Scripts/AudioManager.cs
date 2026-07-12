using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource explorationSource;
    [SerializeField] private AudioSource chaseSource;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.5f;

    // Ces volumes représentent la "balance" voulue pour la musique de cette scène spécifique
    private float _sceneMaxExploVolume = 0.5f;
    private float _sceneMaxChaseVolume = 0.5f;

    private Coroutine _fadeCoroutine;
    private bool _isChased = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
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

        timer = 0f;
        while (timer < fadeDuration / 2f)
        {
            timer += Time.deltaTime;
            float t = timer / (fadeDuration / 2f);
            // On fait le fondu vers le volume max autorisé par la scène
            explorationSource.volume = Mathf.Lerp(0f, _sceneMaxExploVolume, t);
            chaseSource.volume = 0f;
            yield return null;
        }

        explorationSource.volume = _sceneMaxExploVolume;
        chaseSource.volume = 0f;
    }

    public void SetChaseState(bool chaseActive)
    {
        if (_isChased == chaseActive) return;
        _isChased = chaseActive;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        // C'est ici que ça change : le fondu respecte le volume max de la scène
        float targetExplo = _isChased ? 0f : _sceneMaxExploVolume;
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