using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    private ICombatant _owner; // Peut être le joueur ou un ennemi !
    
    private Animator _animator;
    private WeaponDamageDetector _weaponDetector;
    private AttackSO _currentAttackData;

    private bool canCombo;

    private void Awake()
    {
        _owner = GetComponent<ICombatant>();
        _animator = GetComponent<Animator>();
    }
    public void ExecuteAttack(AttackSO attack)
    {
        _currentAttackData = attack;
        canCombo = false;

        _animator.applyRootMotion = true;
        _animator.Play(attack.AnimationHash, attack.animatorLayer, 0f);
        Debug.Log($"AnimationHase : {attack.animationName}");
    }

    public void InterruptAttack()
    {
        if (_currentAttackData == null) return;

        // 1. Fermer la hitbox
        AE_HitboxClose();

        // 2. Stopper le mouvement
        _animator.applyRootMotion = false;

        // 3. Reset l'animation sur son layer
        // On utilise CrossFade pour que ce soit fluide
        _animator.CrossFadeInFixedTime("Empty", 0.1f, _currentAttackData.animatorLayer);

        // IMPORTANT : On remet le poids du layer à 0 
        // pour que le Base Layer (ou celui de la roulade) reprenne le dessus
        _animator.SetLayerWeight(_currentAttackData.animatorLayer, 0f);

        _currentAttackData = null;
        canCombo = false;
    }

    // --- Méthodes appelées par Animation Events ---

    public void AE_EnableCombo()
    {
        canCombo = true;
       // Debug.Log("Fenêtre de combo ouverte !");
    }

    public void AE_HitboxOpen()
    {
        Debug.Log("Fenêtre de dégâts ouverte !");
        Debug.Log($"Owner: {_owner}, AttackData: {_currentAttackData}, WeaponDetector: {_weaponDetector}");
        if (_weaponDetector != null && _currentAttackData != null && _owner != null)
        {
            Debug.Log($"Calcul des dégâts pour {_owner} avec multiplicateur {_currentAttackData.damageMultiplier}");
            // On demande les dégâts de base à l'interface, peu importe qui c'est
            float weaponDamage = _owner.GetBaseWeaponDamage();
            float finalDamage = weaponDamage * _currentAttackData.damageMultiplier;

            _weaponDetector.SetDamageFrame(finalDamage);
            _weaponDetector.ToggleCollider(true);
        }
        else
            Debug.LogWarning("CombatSystem: HitboxOpen appelé sans WeaponDetector, AttackData ou Owner valide !");
    }

    public void AE_HitboxClose()
    {
        _weaponDetector?.DisableDamage();
        _weaponDetector?.ToggleCollider(false);
    }

    public bool CanComboNext() => canCombo;

    public void UpdateWeaponDetector(WeaponDamageDetector newDetector)
    {
        _weaponDetector = newDetector;
    }
}
