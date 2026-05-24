using UnityEngine;

public class SceneMusicConfig : MonoBehaviour
{
    [Header("Musiques uniques de cette Scène")]
    [SerializeField] private AudioClip explorationMusic;
    [SerializeField][Range(0f, 1f)] private float explorationVolume = 0.5f;

    [Space(10)]
    [SerializeField] private AudioClip chaseMusic;
    [SerializeField][Range(0f, 1f)] private float chaseVolume = 0.5f;

    private void Start()
    {
        // Dès que la scène se charge, elle donne ses propres clips à l'AudioManager persistant
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ChangeSceneTracks(explorationMusic, chaseMusic, explorationVolume, chaseVolume);
        }
    }
}