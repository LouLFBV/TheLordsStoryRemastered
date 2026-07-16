using UnityEngine;
using System.Collections;

public class HitBoxWeapon : MonoBehaviour
{
    public ItemData itemData;
    [SerializeField] private GameObject blood;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AI"))
        {
            if (other.TryGetComponent<IDamageable>(out var enemyAI))
            {
                if (itemData.equipmentType != EquipmentType.Arrow)
                {
                   // enemyAI.TakeDamage(itemData.attackPoints, itemData.damageType);
                    //CameraEvents.OnCameraShake?.Invoke(
                    //    itemData.cameraShakeIntensity,
                    //    itemData.cameraShakeDuration
                    //);
                }
                //else if(other.TryGetComponent<EnemyParent>(out var enemyAI1))
                    //StartCoroutine(ApplyArrowEffect(enemyAI1));
            }
            if (blood != null && itemData.equipmentType == EquipmentType.Arrow)
            {
                Instantiate(blood,transform);
            }
        }

        if (itemData.equipmentType == EquipmentType.Arrow)
        {
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }

            transform.position -= transform.forward * 0.1f;

            transform.parent = other.transform;
            if (TryGetComponent<Collider>(out var collider))
                collider.enabled = false;
        }
    }

    //private IEnumerator ApplyArrowEffect(EnemyParent enemyAI)
    //{
    //    switch (itemData.damageType)
    //    {
    //        case DamageType.Feu:
    //            enemyAI.TakeDamage(itemData.attackPoints, itemData.damageType);
    //            for (int i = 0; i < 5; i++)
    //            {
    //                enemyAI.TakeDamage(itemData.attackPoints * 0.2f, itemData.damageType);
    //                yield return new WaitForSeconds(1f);
    //            }
    //            break;

    //        case DamageType.Glace:
    //            enemyAI.TakeDamage(itemData.attackPoints, itemData.damageType);
    //            enemyAI.UpdateSpeedWitchCoefficient(0.5f);
    //            yield return new WaitForSeconds(3f);
    //            enemyAI.UpdateSpeedWitchCoefficient(2f);
    //            break;

    //        case DamageType.Foudre:
    //            enemyAI.TakeDamage(itemData.attackPoints, itemData.damageType);
    //            if (enemyAI.IsDead) yield break;
    //            enemyAI.agent.isStopped = true;
    //            yield return new WaitForSeconds(1.5f);
    //            enemyAI.agent.isStopped = false;
    //            break;

    //        default:
    //            enemyAI.TakeDamage(itemData.attackPoints, itemData.damageType);
    //            CameraEvents.OnCameraShake?.Invoke(
    //                itemData.cameraShakeIntensity,
    //                itemData.cameraShakeDuration
    //            );
    //            break;
    //    }
    //}
}
