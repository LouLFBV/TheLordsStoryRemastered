
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/EnemyData")]
public class NewEnemySO : ScriptableObject
{
    public EnemyType enemyType;

    [Header("Permissions")]
    public bool canOrbit = false;
    public bool canFlank = false;
    public bool isAggressive = true; // Fonce directement ou non
    public bool canBlock = false;

    [Header("LockOn Settings")]
    public float lockOnHeightOffset = 0.5f; 

    [Header("Orbit Settings")]
    public float idealOrbitDistance = 4f;
    public float orbitSpeedMultiplier = 1f;

    [Header("Vision")]
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public float detectionRange = 2f; 

    [Header("Speeds")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolRadius = 8f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 2f;

    [Header("Audio Settings")]
    public AudioClip idleSound;
    public AudioClip walkSound;     // Joué quand il passe en Follow State
    public AudioClip runSound;     // Joué quand il passe en Follow State
    public AudioClip orbitSound;     // Joué quand il passe en Follow State
    public AudioClip hitSound;     // Joué quand il encaisse un coup
    public AudioClip deathSound;   // Joué à sa mort
}


public enum EnemyType
{
    None,
    Loup,
    Squelette,
    Ours,
    Gobelin,
    Boss,
    Ogre,
    ChevalierFantome,
    Araignee,
    Mimic
}