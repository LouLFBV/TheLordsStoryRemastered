using System.Collections;
using UnityEngine;
using TMPro; // Important pour manipuler le TextMeshPro
using UnityEngine.UI; // Pour manipuler les éléments UI comme le CanvasGroup et les Images

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
    [SerializeField] private EndingText[] endingTexts; // Ton tableau personnalisé robuste
    [SerializeField] private float textFadeDuration = 1.5f;   // Vitesse globale des fondus (In/Out) des textes/icônes
    [SerializeField] private GameObject menuButton;           // Le bouton de retour au menu

    private void OnEnable() => bossHealthSystem.OnDeath += StartEndSequence;
    private void OnDisable() => bossHealthSystem.OnDeath -= StartEndSequence;

    private void Start()
    {
        // Initialisation de sécurité : on cache le panneau noir et le bouton
        if (fadePanel != null) fadePanel.alpha = 0f;
        if (menuButton != null) menuButton.SetActive(false);

        // On initialise chaque groupe de texte : tout invisible à l'alpha 0
        foreach (EndingText group in endingTexts)
        {
            if (group == null) continue;

            // 1. On passe tous les TextMeshPro du groupe à un alpha de 0
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

            // 2. On passe l'icône à un alpha de 0
            if (group.icone != null)
            {
                group.icone.color = new Color(group.icone.color.r, group.icone.color.g, group.icone.color.b, 0f);
            }

            // 3. On désactive le parent pour ne pas polluer le raycast ou l'UI au départ
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

        // 1. Désactiver les inputs
        //PlayerController.Instance.Input.DesactiveInput();

        PlayerController.Instance.RequestedPanelType = UIPanelType.EndGame; // On s'assure qu'aucun panneau n'est actif
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);     

        // 2. Prendre le contrôle de la caméra
        ThirdPersonCameraController.Instance.EnterCinematicMode();

        // 3. Rotation fluide vers le ciel
        float time = 0;
        Quaternion startRot = ThirdPersonCameraController.Instance.transform.rotation;

        // --- MAGIE ICI ---
        // On extrait le X (Pitch) du point dans le ciel
        float targetPitch = skyViewPoint.eulerAngles.x;
        // On garde le Y (Yaw) actuel de la caméra pour éviter qu'elle tourne sur les côtés
        float currentYaw = startRot.eulerAngles.y;

        // On reconstruit la rotation cible parfaite (X du ciel, Y actuel, Z à 0)
        Quaternion endRot = Quaternion.Euler(targetPitch, currentYaw, 0f);

        while (time < 2f)
        {
            // Le Slerp va maintenant se faire uniquement sur l'axe X
            ThirdPersonCameraController.Instance.transform.rotation = Quaternion.Slerp(startRot, endRot, time / 2f);
            time += Time.deltaTime;
            yield return null;
        }

        // Attente de 3 secondes avant le fondu au noir
        yield return new WaitForSeconds(1f);

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

        // B. Affichage des groupes de textes un par un
        for (int i = 0; i < endingTexts.Length; i++)
        {
            EndingText currentGroup = endingTexts[i];
            if (currentGroup == null || currentGroup.parentTextObject == null) continue;

            // On active le conteneur parent
            currentGroup.parentTextObject.SetActive(true);

            // Fondu d'apparition (Alpha 0 -> 1) de tous les textes + l'icône en même temps
            yield return FadeEndingGroup(currentGroup, 1f, textFadeDuration);

            // Temps d'attente propre à cette étape (configuré dans ton inspecteur)
            yield return new WaitForSeconds(currentGroup.displayDuration);

            // SI ce n'est PAS la dernière étape, on fait tout disparaître
            if (i < endingTexts.Length - 1)
            {
                yield return FadeEndingGroup(currentGroup, 0f, textFadeDuration);
                currentGroup.parentTextObject.SetActive(false); // Nettoyage de la hiérarchie
            }
            // SI c'est le dernier groupe, la boucle s'arrête et il reste affiché !
        }

        // C. Affichage du bouton de retour au menu
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

    // --- NOUVELLE FONCTION : FONDU SIMULTANÉ DU GROUPE (TEXTES + ICÔNE) ---
    private IEnumerator FadeEndingGroup(EndingText group, float targetAlpha, float duration)
    {
        float time = 0f;

        // On sauvegarde les alphas de départ pour chaque texte du groupe
        float[] startAlphasText = new float[group.textMeshProUGUIs.Length];
        for (int j = 0; j < group.textMeshProUGUIs.Length; j++)
        {
            if (group.textMeshProUGUIs[j] != null)
                startAlphasText[j] = group.textMeshProUGUIs[j].color.a;
        }

        // On sauvegarde l'alpha de départ de l'icône
        float startAlphaIcon = group.icone != null ? group.icone.color.a : 0f;

        while (time < duration)
        {
            float progress = time / duration;

            // Appliquer le fondu sur tous les textes présents dans le tableau
            for (int j = 0; j < group.textMeshProUGUIs.Length; j++)
            {
                if (group.textMeshProUGUIs[j] != null)
                {
                    Color c = group.textMeshProUGUIs[j].color;
                    c.a = Mathf.Lerp(startAlphasText[j], targetAlpha, progress);
                    group.textMeshProUGUIs[j].color = c;
                }
            }

            // Appliquer le fondu sur l'icône (si elle existe)
            if (group.icone != null)
            {
                Color c = group.icone.color;
                c.a = Mathf.Lerp(startAlphaIcon, targetAlpha, progress);
                group.icone.color = c;
            }

            time += Time.deltaTime;
            yield return null;
        }

        // Sécurité de fin de boucle : on force l'alpha cible exact
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