using System;
using UnityEngine;

public class PlayerController : MonoBehaviour, ICombatant
{
    public static PlayerController Instance { get; private set; }

    [Header("Core Components")]
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerInputHandler Input { get; private set; }
    public PlayerCharacterMotor Motor { get; private set; }
    public Animator Animator { get; private set; }
    public Rigidbody Rigidbody { get; private set; }

    [Header("States")]
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerRollState RollState { get; private set; }
    public PlayerHitState HitState { get; private set; }
    public PlayerStunnedState StunnedState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerAimState AimState { get; private set; }
    public PlayerEquipState EquipState { get; private set; }
    public PlayerUnequipState UnequipState { get; private set; }
    public PlayerBowChargeState BowChargeState { get; private set; }
    public PlayerCrouchState CrouchState { get; private set; }
    public PlayerDeathState DeathState { get; private set; }
    public PlayerUIState UIState { get; private set; }


    [Header("Systems")]
    public HealthSystem Health { get; private set; }
    public ArmorSystem Armor { get; private set; }
    public PoiseSystem Poise { get; private set; }
    public DamageReceiver DmgReceiver { get; private set; }
    public BowBehaviour Bow { get; private set; }
    public LockOnSystem LockOn { get; private set; }
    public WalletSystem Wallet { get; private set; }
    public CombatSystem Combat { get; private set; }
    public StaminaSystem Stamina { get; private set; }


    [Header("Combat Settings")]
    public bool usingSpecialAttack = false;
    public AttackSO CurrentAttack { get; set; }
    public ItemData PendingWeaponItem { get; set; }
    public HandWeapon PendingWeaponType { get; set; }
    public HandWeapon PendingUnequipType { get; set; }

    [Header("Jump Settings")]
    public float jumpCooldown = 0.5f; // Temps entre deux sauts
    public float lastJumpTime; // Stocke le moment du dernier saut

    [Header("Roll Settings")]
    public float rollForce = 4f;


    [Header("Library")]
    public EquipmentLibrary equipmentLibrary;
    public EquipmentLibraryItem PendingLibraryItem { get; private set; }

    [Header("Others")]
    [SerializeField] private InteractSystem interactSystem;
    public UIPanelType RequestedPanelType { get; set; }
    public event Action<bool> OnOpenUI;
    private UIPanelType? _previousPanelType = null;
    public ItemData ItemQueuedToEquip;
    public bool IsDead { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Input = GetComponent<PlayerInputHandler>();
        Motor = GetComponent<PlayerCharacterMotor>();
        Health = GetComponent<HealthSystem>();
        Armor = GetComponent<ArmorSystem>();
        Stamina = GetComponent<StaminaSystem>();
        Animator = GetComponent<Animator>();
        Combat = GetComponent<CombatSystem>();
        Rigidbody = GetComponent<Rigidbody>();
        DmgReceiver = GetComponent<DamageReceiver>();
        Poise = GetComponent<PoiseSystem>();
        Bow = GetComponent<BowBehaviour>();
        LockOn = GetComponent<LockOnSystem>();
        Wallet = GetComponent<WalletSystem>();
        IdleState = new PlayerIdleState(this);
        MoveState = new PlayerMoveState(this);
        AttackState = new PlayerAttackState(this);
        RollState = new PlayerRollState(this);
        HitState = new PlayerHitState(this);
        StunnedState = new PlayerStunnedState(this);
        FallState = new PlayerFallState(this);
        JumpState = new PlayerJumpState(this);
        CrouchState = new PlayerCrouchState(this);
        DeathState = new PlayerDeathState(this);
        AimState = new PlayerAimState(this);
        EquipState = new PlayerEquipState(this);
        UnequipState = new PlayerUnequipState(this);
        BowChargeState = new PlayerBowChargeState(this);
        UIState = new PlayerUIState(this);

        StateMachine = new PlayerStateMachine(
            new System.Collections.Generic.Dictionary<PlayerStateType, PlayerState>
            {
                { PlayerStateType.Idle, IdleState},
                { PlayerStateType.Move, MoveState},
                { PlayerStateType.Attack, AttackState},
                { PlayerStateType.Roll, RollState},
                { PlayerStateType.Hit, HitState},
                { PlayerStateType.Stunned, StunnedState},
                { PlayerStateType.Jump, JumpState},
                { PlayerStateType.Fall, FallState},
                { PlayerStateType.Death, DeathState},
                { PlayerStateType.Aim, AimState},
                { PlayerStateType.Equip, EquipState},
                { PlayerStateType.Unequip, UnequipState},
                { PlayerStateType.Crouch, CrouchState},
                { PlayerStateType.UI, UIState},
                { PlayerStateType.BowCharge, BowChargeState}
            }
        );
    }

    private void Start()
    {
        StateMachine.Initialize(PlayerStateType.Idle);
        RequestedPanelType = UIPanelType.None;
    }


    private void Update()
    {
        StateMachine.Update();
        PaletteSystem.instance.HandlePaletteLogic(this);

        // --- LOGIQUE D'OUVERTURE ---
        if (Input.MenuPressed || Input.InventoryPressed)
        {
            Debug.Log("Menu ou Inventaire Pressé - Tentative d'ouverture de l'UI");
            UIPanelType newType = Input.MenuPressed ? UIPanelType.PauseMenu : UIPanelType.Inventory;

            // SI on est déjà en dialogue, on sauvegarde cet état
            if (StateMachine.CurrentState == UIState && RequestedPanelType == UIPanelType.Dialogue)
            {
                _previousPanelType = UIPanelType.Dialogue;
            }

            RequestedPanelType = newType;
            StateMachine.ChangeState(PlayerStateType.UI);
            Input.UseMenuInput();
            Input.UseInventoryInput();
        }

        // --- LOGIQUE DE FERMETURE ---
        else if (StateMachine.CurrentState == UIState)
        {
            if (Input.CloseMenuPressed || Input.CloseInventoryPressed)
            {
                // Si on avait un dialogue en cours avant la pause
                if (_previousPanelType == UIPanelType.Dialogue)
                {
                    RequestedPanelType = UIPanelType.Dialogue;
                    _previousPanelType = null;

                    // On force le rafraîchissement de l'UIState sans repasser par Idle
                    UIState.Enter();
                }
                else if (RequestedPanelType != UIPanelType.Dialogue)
                {
                    StateMachine.ChangeState(PlayerStateType.Idle);
                }

                Input.UseCloseInventoryInput();
                Input.UseCloseMenuInput();
            }
        }
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    private void OnAnimatorMove()
    {
        if (!Animator.applyRootMotion)
            return;

        Rigidbody.MovePosition(
            Rigidbody.position + Animator.deltaPosition
        );

        Rigidbody.MoveRotation(
            Rigidbody.rotation * Animator.deltaRotation
        );
    }

    #region Animation Events
    public void AE_EquipWeapon() // AE pour Animation Event
    {
        // On délègue la logique à l'état actuel si c'est un état d'équipement
        if (StateMachine.CurrentState is PlayerEquipState equip)
        {            
            equip.HandleWeaponSwitch();
        }
    }

    public void AE_UnequipWeapon()
    {
        if (StateMachine.CurrentState is PlayerUnequipState unequip)
        {
            unequip.HandleWeaponRemoval();
        }
    }

    public void AE_OnAttackFinished()
    {
        if (StateMachine.CurrentState is PlayerAttackState attack)
        {
            attack.OnAnimationFinished();
        }
    }
    public void AE_OnRollEnd()
    {
        if (StateMachine.CurrentState is PlayerRollState rollState)
        {
            rollState.OnRollAnimationEnd();
        }
    }
    public void AE_OnHitAnimationEnd()
    {
        if (StateMachine.CurrentState is PlayerHitState hitState)
        {
            hitState.OnHitAnimationEnd();
        }
    }
    public void AE_PlayHarvestingSoundEffectFromInteractBehaviour() => interactSystem.PlayHarvestingSoundEffect();

    public void AE_BreakHarvestableFromInteractBehaviour() => StartCoroutine(interactSystem.BreakHarvestable());


    public void AE_StopBusyState()
    {
        interactSystem.isBusy = false;
    }
    #endregion
    public void PrepareEquip(ItemData data)
    {
        // On cherche l'item correspondant dans la librairie
        PendingLibraryItem = equipmentLibrary.Get(data);

        if (PendingLibraryItem != null)
        {
            PendingWeaponItem = data;

            PendingWeaponType = data.handWeaponType;
            StateMachine.ChangeState(PlayerStateType.Equip);
        }
    }

    public float GetBaseWeaponDamage() => PendingWeaponItem.attackPoints;

    public void OnOpenUIEvent(bool isOpen)
    {
        OnOpenUI?.Invoke(isOpen);
    }

    private void OnEnable()
    {
        if (Health != null)
            Health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (Health != null)
            Health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (!IsDead) 
        StateMachine.ChangeState(PlayerStateType.Death);
    }

    #region Save Sytem

    public PlayerControllerSaveData GetSaveData()
    {
        return new PlayerControllerSaveData
        {
            currentHealth = Health.CurrentHealth,
            currentEndurance = Stamina.CurrentStamina,
            gold = Wallet.GetGoldAmount(),

            position = transform.position,

        };
    }

    public void LoadSaveData(PlayerControllerSaveData data)
    {
        Health.SetHealth(data.currentHealth);
        Stamina.SetStamania(data.currentEndurance);
        Wallet.SetGoldAmount(data.gold);

        transform.position = data.position;
    }

    #endregion


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"State: {StateMachine.CurrentState.GetType().Name}");
        }
    }
#endif
}


[System.Serializable]
public class PlayerControllerSaveData
{
    public float currentHealth;
    public float currentEndurance;
    public int gold;

    public Vector3 position;
}
