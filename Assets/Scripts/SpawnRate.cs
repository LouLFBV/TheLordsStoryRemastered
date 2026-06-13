using UnityEngine;

public class SpawnRate : MonoBehaviour
{
    [Header("Spawn Rate Settings (0 -> 100)")]
    [SerializeField] private int spawnRate; 
    void Start()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue > spawnRate)
        {
            Destroy(transform.gameObject);
        }
    }
}
