using UnityEngine;

public class DamageInfo
{
    public float rawPhysicalDamage;
    public DamageType physicalType;
    public Effet elementalEffect;
    public float poiseDamage;
    public float stunDuration;
    public GameObject attacker;

    public DamageInfo(float damage, DamageType pType, Effet elem, float poise, float stun, GameObject attackerInstance)
    {
        rawPhysicalDamage = damage;
        physicalType = pType;
        elementalEffect = elem;
        poiseDamage = poise;
        stunDuration = stun;
        attacker = attackerInstance;
    }
}