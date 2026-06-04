using UnityEngine;

public class AIManager : MonoBehaviour
{
    private EnemyControllerBase _enemy;
    [SerializeField] private NewEnemySO enemyData; // Ta base de données
    private float _orbitCooldownTimer;
    [SerializeField] private float defaultCooldown = 5f; // Temps entre deux orbites

    public void Initialize(EnemyControllerBase owner)
    {
        _enemy = owner;
    }

    // Système de permission simple
    public bool CanOrbit => enemyData != null && enemyData.canOrbit;
    public bool CanBlock => enemyData != null && enemyData.canBlock;
    public bool CanGetHit => enemyData != null && enemyData.canGetHit;
    public bool HasGetHitAnim => enemyData != null && enemyData.hasGetHitAnim;
    public bool HasStunnedAnim => enemyData != null && enemyData.hasStunnedAnim;

    // Accès aux réglages du SO pour les états
    public float OrbitDistance => enemyData != null ? enemyData.idealOrbitDistance : 4f;
    public float OrbitSpeed => enemyData != null ? enemyData.orbitSpeedMultiplier : 1f;

    public NewEnemySO GetData() => enemyData;

    public bool HasPermission(EnemyStateType stateType)
    {
        if (enemyData == null) return false;

        if (stateType == EnemyStateType.Orbit)
        {
            // L'ours ne peut orbiter que si le cooldown est à 0
            return enemyData.canOrbit && _orbitCooldownTimer <= 0;
        }
        return true;
    }

    public void StartOrbitCooldown(float duration = -1)
    {
        // Si on ne précise pas de durée, on prend celle par défaut
        _orbitCooldownTimer = (duration < 0) ? defaultCooldown : duration;
    }

    private void Update()
    {
        if (_orbitCooldownTimer > 0)
        {
            _orbitCooldownTimer -= Time.deltaTime;
        }
    }
}