using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameEndManager : MonoBehaviour
{
    [Header("Cinematic & Camera")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private Transform skyViewPoint;

    [Header("References")]
    [SerializeField] private HealthSystem bossHealthSystem;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip endGameMusic;
    [SerializeField] private float musicFadeDuration = 3f; // Durée totale du fondu (Fade Out + Fade In)

    [Header("Ending UI (Séquence de Textes)")]
    [SerializeField] private EndingText[] endingTexts;
    [SerializeField] private float textFadeDuration = 1.5f;
    [SerializeField] private GameObject menuButton;

    private float _originalVolume = 1f; // Permet de retenir le volume initial configuré sur l'AudioSource

    private void OnEnable() => bossHealthSystem.OnDeath += StartEndSequence;
    private void OnDisable() => bossHealthSystem.OnDeath -= StartEndSequence;

    private void Start()
    {
        // On sauvegarde le volume de base de ton inspecteur pour y retourner après le fondu
        if (audioSource != null) _originalVolume = audioSource.volume;

        if (fadePanel != null) fadePanel.alpha = 0f;
        if (menuButton != null) menuButton.SetActive(false);

        foreach (EndingText group in endingTexts)
        {
            if (group == null) continue;

            if (group.textMeshProUGUIs != null)
            {
                foreach (TextMeshProUGUI tmp in group.textMeshProUGUIs)
                {
                    if (tmp != null)
                    {
                        tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0f);
                    }
                }
            }

            if (group.icone != null)
            {
                group.icone.color = new Color(group.icone.color.r, group.icone.color.g, group.icone.color.b, 0f);
            }

            if (group.parentTextObject != null)
            {
                group.parentTextObject.SetActive(false);
            }
        }
    }

    private void StartEndSequence()
    {
        StartCoroutine(EndSequenceRoutine());
    }

    private IEnumerator EndSequenceRoutine()
    {
        
        yield return new WaitForSeconds(5f);

        PlayerController.Instance.RequestedPanelType = UIPanelType.EndGame;
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);

        ThirdPersonCameraController.Instance.EnterCinematicMode();

        float time = 0;
        Quaternion startRot = ThirdPersonCameraController.Instance.transform.rotation;

        float targetPitch = skyViewPoint.eulerAngles.x;
        float currentYaw = startRot.eulerAngles.y;

        Quaternion endRot = Quaternion.Euler(targetPitch, currentYaw, 0f);

        while (time < 2f)
        {
            ThirdPersonCameraController.Instance.transform.rotation = Quaternion.Slerp(startRot, endRot, time / 2f);
            time += Time.deltaTime;
            yield return null;
        }

        if (audioSource != null && endGameMusic != null)
        {
            // On utilise StartCoroutine SANS "yield return" pour que le fondu de la musique
            // se fasse en tâche de fond pendant que les textes s'affichent à l'écran !
            StartCoroutine(FadeAudioTransition(endGameMusic, musicFadeDuration));
        }
        yield return new WaitForSeconds(1f);

        yield return FadeCanvas(fadePanel, 1f, 2f);

        ShowEndingUI();
    }

    private void ShowEndingUI()
    {
        StartCoroutine(EndingSequenceUIFlow());
    }

    private IEnumerator EndingSequenceUIFlow()
    {

        // B. Affichage des groupes de textes un par un
        for (int i = 0; i < endingTexts.Length; i++)
        {
            EndingText currentGroup = endingTexts[i];
            if (currentGroup == null || currentGroup.parentTextObject == null) continue;

            currentGroup.parentTextObject.SetActive(true);

            yield return FadeEndingGroup(currentGroup, 1f, textFadeDuration);

            yield return new WaitForSeconds(currentGroup.displayDuration);

            if (i < endingTexts.Length - 1)
            {
                yield return FadeEndingGroup(currentGroup, 0f, textFadeDuration);
                currentGroup.parentTextObject.SetActive(false);
            }
        }

        if (menuButton != null)
        {
            menuButton.SetActive(true);
        }
    }

    // --- NOUVELLE FONCTION : FONDU AUDIO DE TRANSITION ---
    private IEnumerator FadeAudioTransition(AudioClip newClip, float duration)
    {
        float halfDuration = duration / 2f;
        float startVolume = audioSource.volume;
        float time = 0f;

        // 1. FADE OUT (Disparition de l'ancienne musique)
        while (time < halfDuration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / halfDuration);
            time += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();

        // 2. CHANGEMENT DE MORCEAU
        audioSource.clip = newClip;
        audioSource.loop = true;
        audioSource.Play();

        // 3. FADE IN (Apparition de la nouvelle musique)
        time = 0f;
        while (time < halfDuration)
        {
            audioSource.volume = Mathf.Lerp(0f, _originalVolume, time / halfDuration);
            time += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = _originalVolume; // On s'assure d'être calé au volume cible exact
    }

    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadeEndingGroup(EndingText group, float targetAlpha, float duration)
    {
        float time = 0f;

        float[] startAlphasText = new float[group.textMeshProUGUIs.Length];
        for (int j = 0; j < group.textMeshProUGUIs.Length; j++)
        {
            if (group.textMeshProUGUIs[j] != null)
                startAlphasText[j] = group.textMeshProUGUIs[j].color.a;
        }

        float startAlphaIcon = group.icone != null ? group.icone.color.a : 0f;

        while (time < duration)
        {
            float progress = time / duration;

            for (int j = 0; j < group.textMeshProUGUIs.Length; j++)
            {
                if (group.textMeshProUGUIs[j] != null)
                {
                    Color c = group.textMeshProUGUIs[j].color;
                    c.a = Mathf.Lerp(startAlphasText[j], targetAlpha, progress);
                    group.textMeshProUGUIs[j].color = c;
                }
            }

            if (group.icone != null)
            {
                Color c = group.icone.color;
                c.a = Mathf.Lerp(startAlphaIcon, targetAlpha, progress);
                group.icone.color = c;
            }

            time += Time.deltaTime;
            yield return null;
        }

        foreach (var tmp in group.textMeshProUGUIs)
        {
            if (tmp != null) tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, targetAlpha);
        }
        if (group.icone != null)
        {
            group.icone.color = new Color(group.icone.color.r, group.icone.color.g, group.icone.color.b, targetAlpha);
        }
    }
}

[System.Serializable]
public class EndingText
{
    public GameObject parentTextObject; // Le conteneur UI principal de cette étape
    public TextMeshProUGUI[] textMeshProUGUIs; // Tous les textes à animer en même temps (ex: Titre + Paragraphe)
    public Image icone; // L'icône ou le logo lié à ce texte
    public float displayDuration = 3f; // Durée d'affichage spécifique
}