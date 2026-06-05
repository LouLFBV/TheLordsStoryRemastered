
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/EnemyData")]
public class NewEnemySO : ScriptableObject
{
    public EnemyType enemyType;

    [Header("Permissions")]
    public bool canOrbit = false;
    public bool canBlock = false;
    public bool canGetHit = true;
    public bool hasGetHitAnim = false;
    public bool hasStunnedAnim = false;

    [Header("Combat Settings")]
    public float blockDuration = 2f; 
    public float blockCooldown = 5f; 
    public float distToStartBlock = 3.5f;

    [Header("LockOn Settings")]
    public float lockOnHeightOffset = 0.5f;
    public float maxRangeLockOn = 20f;

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
    public AudioClip walkSound;    
    public AudioClip runSound;     
    public AudioClip orbitSound;     
    public AudioClip[] hitSound;     
    public AudioClip deathSound;   
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