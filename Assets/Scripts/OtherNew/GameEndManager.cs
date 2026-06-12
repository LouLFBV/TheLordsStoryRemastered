using System.Collections;
using UnityEngine;
using TMPro; // Important pour manipuler le TextMeshPro

public class GameEndManager : MonoBehaviour
{
    [Header("Cinematic & Camera")]
    [SerializeField] private CanvasGroup fadePanel; // Le panneau noir en UI
    [SerializeField] private Transform skyViewPoint; // Un transform vide dans le ciel

    [Header("References")]
    [SerializeField] private HealthSystem bossHealthSystem;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip endGameMusic;

    [Header("Ending UI (Séquence de Textes)")]
    [SerializeField] private EndingText[] endingTexts; // Ton tableau de GameObjects contenant du TMPro
    [SerializeField] private GameObject menuButton;           // Le bouton de retour au menu

    private void OnEnable() => bossHealthSystem.OnDeath += StartEndSequence;
    private void OnDisable() => bossHealthSystem.OnDeath -= StartEndSequence;

    private void Start()
    {
        // Initialisation de sécurité : on cache le panneau noir et le bouton
        if (fadePanel != null) fadePanel.alpha = 0f;
        if (menuButton != null) menuButton.SetActive(false);

        // On éteint tous les textes du tableau et on met leur alpha à 0
        foreach (EndingText txtObject in endingTexts)
        {
            if (txtObject != null)
            {
                TextMeshProUGUI tmp = txtObject.textObject.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0f);
                }
                txtObject.textObject.SetActive(false);
            }
        }
    }

    private void StartEndSequence()
    {
        StartCoroutine(EndSequenceRoutine());
    }

    private IEnumerator EndSequenceRoutine()
    {
        // 1. Désactiver les inputs
        PlayerController.Instance.Input.DesactiveInput();

        // 2. Prendre le contrôle de la caméra
        ThirdPersonCameraController.Instance.EnterCinematicMode();

        // 3. Rotation fluide vers le ciel
        float time = 0;
        Quaternion startRot = ThirdPersonCameraController.Instance.transform.rotation;
        Quaternion endRot = skyViewPoint.rotation;

        while (time < 2f)
        {
            ThirdPersonCameraController.Instance.transform.rotation = Quaternion.Slerp(startRot, endRot, time / 2f);
            time += Time.deltaTime;
            yield return null;
        }

        // --- Attente de 3 secondes avant le fondu au noir ---
        yield return new WaitForSeconds(3f);

        // 4. Fondu au noir (durée : 2 secondes)
        yield return FadeCanvas(fadePanel, 1f, 2f);

        // 5. Lancer la gestion des textes
        ShowEndingUI();
    }

    private void ShowEndingUI()
    {
        StartCoroutine(EndingSequenceUIFlow());
    }

    private IEnumerator EndingSequenceUIFlow()
    {
        // A. Changement de musique
        if (audioSource != null && endGameMusic != null)
        {
            audioSource.Stop();
            audioSource.clip = endGameMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        // B. Affichage des textes un par un
        for (int i = 0; i < endingTexts.Length; i++)
        {
            if (endingTexts[i] == null) continue;

            // Récupération du composant TextMeshPro
            if (!endingTexts[i].textObject.TryGetComponent<TextMeshProUGUI>(out var tmpText))
            {
                Debug.LogWarning($"Le GameObject {endingTexts[i].textObject.name} n'a pas de composant TextMeshProUGUI !");
                continue;
            }

            // On active le GameObject et on lance le fondu d'apparition (Alpha 0 -> 1)
            endingTexts[i].textObject.SetActive(true);
            yield return FadeTMPro(tmpText, 1f, endingTexts[i].displayDuration);

            yield return new WaitForSeconds(endingTexts[i].displayDuration);

            // SI ce n'est PAS le dernier texte du tableau, on le fait disparaître
            if (i < endingTexts.Length - 1)
            {
                yield return FadeTMPro(tmpText, 0f, endingTexts[i].displayDuration);
                endingTexts[i].textObject.SetActive(false); // On le désactive pour nettoyer la hiérarchie
            }
            // SI c'est le dernier texte, la boucle se termine ici, il reste donc affiché !
        }

        // C. Affichage du bouton de retour au menu (une fois la boucle terminée)
        if (menuButton != null)
        {
            menuButton.SetActive(true);
        }
    }

    // --- FONCTION DE FONDU POUR CANVAS GROUP (PANNEAU NOIR) ---
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

    // --- FONCTION DE FONDU POUR TEXTMESHPRO ---
    private IEnumerator FadeTMPro(TextMeshProUGUI text, float targetAlpha, float duration)
    {
        Color startColor = text.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        float time = 0f;

        while (time < duration)
        {
            text.color = Color.Lerp(startColor, targetColor, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        text.color = targetColor;
    }
}

[System.Serializable]
public class EndingText
{
    public GameObject textObject; // Le GameObject contenant le TextMeshPro
    public float displayDuration = 3f; // Durée d'affichage pour ce texte spécifique
}