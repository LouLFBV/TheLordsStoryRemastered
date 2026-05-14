using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyController : EnemyControllerBase
{
}

[System.Serializable]
public class EnemyWeaponSetup
{
    public ItemData weaponData;          // Contient les points d'attaque (ex: 15)
    public WeaponDamageDetector detector; // Le script sur l'objet physique
    public List<AttackSO> usableAttacks;  // Les attaques que CETTE arme peut faire
}