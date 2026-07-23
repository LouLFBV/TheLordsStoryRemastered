using System;
using System.Collections;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    public static ThirdPersonCameraController Instance { get; private set; }

    [Header("Target")]
    private PlayerController _playerController;
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 10f;

    [Header("Orbit Settings")]
    [SerializeField] private Vector3 defaultPivotOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField] private Vector3 defaultCamOffset = new Vector3(0f, 0f, -3f);

    [Header("FOV Settings")]
    public float SprintFOV { get; private set; }
    [SerializeField] private float sprintFOV = 80f;
    [SerializeField] private float fovLerpSpeed = 8f;

    [Header("Aim Settings")]
    [SerializeField] private Vector3 aimPivotOffset = new Vector3(0.5f, 1.7f, 0f);
    [SerializeField] private Vector3 aimCamOffset = new Vector3(0f, 0f, -1.2f);
    public Vector3 AimPivotOffset => aimPivotOffset;
    public Vector3 AimCamOffset => aimCamOffset;
    public Vector3 DefaultCamOffset => defaultCamOffset;

    [Header("Vertical Clamp")]
    [SerializeField] private float minVerticalAngle = -40f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float cameraRadius = 0.2f;
    [SerializeField] private float minCollisionDistance = 0.3f;
    [SerializeField] private float collisionSmoothSpeed = 15f;
    private float currentCollisionDistance;

    private bool _isLocked = false;
    private bool _isCinematicMode = false;
    public bool IsLocked => _isLocked;

    private bool isAiming = false;

    // Valeurs de travail
    private Vector3 currentPivotOffset;
    private Vector3 currentCamOffset;
    private Vector3 targetPivotOffset;
    private Vector3 targetCamOffset;

    private float targetFOV;
    private float defaultFOV;
    public float DefaultFOV => defaultFOV;
    private Camera _camComponent;
    private PlayerInputHandler input;
    public float GetYaw() => yaw;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        input = target.GetComponent<PlayerInputHandler>();
        _camComponent = GetComponent<Camera>();
        _playerController = target.GetComponent<PlayerController>();

        defaultFOV = _camComponent.fieldOfView;
        targetFOV = defaultFOV;
        SprintFOV = sprintFOV;

        currentPivotOffset = targetPivotOffset = defaultPivotOffset;
        currentCamOffset = targetCamOffset = defaultCamOffset;

        yaw = target.eulerAngles.y;
        currentCollisionDistance = defaultCamOffset.magnitude;
    }

    private void Start()
    {
        _isLocked = false;
    }

    private void LateUpdate()
    {
        if (_isLocked || _isCinematicMode) return;
        HandleInput();
        UpdateCameraPosition();
    }

    private void OnEnable()
    {
        CameraEvents.OnCameraShake += PlayCameraShake;
    }

    private void OnDisable()
    {
        CameraEvents.OnCameraShake -= PlayCameraShake;
    }

    private void HandleInput()
    {
        if (_playerController.LockOn != null && _playerController.LockOn.IsLocked)
        {
            UpdateLockOnRotation();
            return;
        }
        Vector2 look = input.MouseLook + input.GamepadLook;
        yaw += look.x * Time.deltaTime;
        pitch -= look.y * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void UpdateLockOnRotation()
    {
        Transform targetEnemy = _playerController.LockOn.CurrentTarget;
        if (targetEnemy == null) return;

        Vector3 targetPoint;
        if (targetEnemy.TryGetComponent<EnemyControllerBase>(out var enemy))
        {
            targetPoint = targetEnemy.position + Vector3.up * enemy.enemyData.lockOnHeightOffset;
        }
        else
        {
            targetPoint = targetEnemy.position + Vector3.up * 0.5f;
        }
        Vector3 dir = (targetPoint - transform.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        float targetYaw = targetRot.eulerAngles.y;
        float targetPitch = targetRot.eulerAngles.x;

        if (targetPitch > 180) targetPitch -= 360f;

        yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * smoothTime);
        pitch = Mathf.LerpAngle(pitch, targetPitch, Time.deltaTime * smoothTime);
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void UpdateCameraPosition()
    {
        // En visée, on calcule le pivot idéal en fonction des murs environnants
        if (isAiming)
        {
            targetPivotOffset = GetSafeAimPivotOffset();
            targetCamOffset = aimCamOffset;
        }

        // 1. Interpolation fluide des offsets
        currentPivotOffset = Vector3.Lerp(currentPivotOffset, targetPivotOffset, Time.deltaTime * smoothTime);
        currentCamOffset = Vector3.Lerp(currentCamOffset, targetCamOffset, Time.deltaTime * smoothTime);
        _camComponent.fieldOfView = Mathf.Lerp(_camComponent.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

        // 2. Calcul des rotations
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Quaternion yawRot = Quaternion.Euler(0, yaw, 0);

        // 3. Calcul du pivot central du joueur (sécurisé)
        Vector3 centerHeadPos = target.position + yawRot * new Vector3(0f, currentPivotOffset.y, 0f);

        // Sécurité supplémentaire : s'assurer que le pivot ne traverse aucun mur depuis la tête
        Vector3 desiredPivotPos = target.position + yawRot * currentPivotOffset;
        Vector3 pivotDir = desiredPivotPos - centerHeadPos;
        Vector3 safePivotPos = desiredPivotPos;

        if (pivotDir.sqrMagnitude > 0.001f)
        {
            if (Physics.SphereCast(centerHeadPos, cameraRadius, pivotDir.normalized, out RaycastHit pivotHit, pivotDir.magnitude, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                safePivotPos = centerHeadPos + pivotDir.normalized * Mathf.Max(0, pivotHit.distance - cameraRadius);
            }
        }

        // 4. GESTION DES COLLISIONS DURECUL DE LA CAMÉRA
        float maxDistance = currentCamOffset.magnitude;
        Vector3 direction = rot * Vector3.back;

        float targetDistance = maxDistance;

        if (Physics.SphereCast(safePivotPos, cameraRadius, direction, out RaycastHit hit, maxDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            targetDistance = Mathf.Clamp(hit.distance - cameraRadius, minCollisionDistance, maxDistance);
        }

        currentCollisionDistance = Mathf.Lerp(currentCollisionDistance, targetDistance, Time.deltaTime * collisionSmoothSpeed);

        // 5. Calcul et application de la position finale
        Vector3 finalPos = safePivotPos + direction * currentCollisionDistance;

        transform.position = finalPos + rot * shakeOffset;
        transform.rotation = rot;
    }

    /// <summary>
    /// Choisit l'épaule droite, l'épaule gauche, ou le centre selon les obstacles dans le coin.
    /// </summary>
    private Vector3 GetSafeAimPivotOffset()
    {
        Quaternion yawRot = Quaternion.Euler(0, yaw, 0);
        Vector3 headPos = target.position + yawRot * new Vector3(0f, aimPivotOffset.y, 0f);
        float shoulderOffset = Mathf.Abs(aimPivotOffset.x);

        // Test 1 : Épaule droite dégagée ?
        bool rightBlocked = Physics.SphereCast(headPos, cameraRadius, yawRot * Vector3.right, out _, shoulderOffset, collisionLayers, QueryTriggerInteraction.Ignore);

        if (!rightBlocked)
        {
            return new Vector3(shoulderOffset, aimPivotOffset.y, aimPivotOffset.z);
        }

        // Test 2 : Épaule gauche dégagée ?
        bool leftBlocked = Physics.SphereCast(headPos, cameraRadius, yawRot * Vector3.left, out _, shoulderOffset, collisionLayers, QueryTriggerInteraction.Ignore);

        if (!leftBlocked)
        {
            return new Vector3(-shoulderOffset, aimPivotOffset.y, aimPivotOffset.z);
        }

        // Test 3 : Si les deux épaules sont bloquées (coin étroit), on recentre le pivot (X = 0)
        return new Vector3(0f, aimPivotOffset.y, aimPivotOffset.z);
    }

    public Transform GetTransform() => transform;
    public void EnterCinematicMode() { _isCinematicMode = true; }
    public void ExitCinematicMode() { _isCinematicMode = false; }

    public void SetAimState(bool aiming)
    {
        isAiming = aiming;

        if (!isAiming)
        {
            targetPivotOffset = defaultPivotOffset;
            targetCamOffset = defaultCamOffset;
        }
    }

    public void SetManualOffsets(Vector3 pivot, Vector3 cam)
    {
        targetPivotOffset = pivot;
        targetCamOffset = cam;
    }
    public void SetFOV(float fov) => targetFOV = fov;
    public void ResetFOV() => targetFOV = defaultFOV;
    public void LockCamera() => _isLocked = true;
    public void UnlockCamera() => _isLocked = false;

    public void SetLockCamera(bool locked)
    {
        _isLocked = locked;
    }

    public void SetRotation(float newYaw, float newPitch)
    {
        yaw = newYaw;
        pitch = newPitch;
    }

    #region CameraShake
    private Coroutine shakeRoutine;
    private Vector3 shakeOffset;

    private void PlayCameraShake(float intensity, float duration)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(CameraShake(intensity, duration));
    }

    private IEnumerator CameraShake(float intensity, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float strength = Mathf.Lerp(intensity, 0f, time / duration);
            shakeOffset = UnityEngine.Random.insideUnitSphere * strength;
            shakeOffset = Vector3.ClampMagnitude(shakeOffset, intensity);

            yield return null;
        }
        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }
    #endregion
}

public static class CameraEvents
{
    public static Action<float, float> OnCameraShake;
    // amplitude, duration
}