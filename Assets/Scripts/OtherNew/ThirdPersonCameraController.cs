using System;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    public static ThirdPersonCameraController Instance { get; private set; }

    [Header("Target")]
    private PlayerController _playerController;
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 10f;

    [Header("Orbit Settings")]
    //[SerializeField] private float defaultDistance = 3f;
    [SerializeField] private Vector3 defaultPivotOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField] private Vector3 defaultCamOffset = new Vector3(0f, 0f, -3f);
    //[SerializeField] private float distance = 3f; // Gardé selon ta demande
    //[SerializeField] private float height = 1.7f; // Gardé selon ta demande

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
    [SerializeField] private float minCollisionDistance = 0.5f; 
    [SerializeField] private float collisionSmoothSpeed = 12f; // Pour un retour fluide après une collision
    private float currentCollisionDistance;


    [Header("Lock Transition")]
    private bool _isLocked = false;
    public bool IsLocked => _isLocked;

    // Valeurs de travail
    private Vector3 currentPivotOffset;
    private Vector3 currentCamOffset;
    private Vector3 targetPivotOffset;
    private Vector3 targetCamOffset;
    private Vector3 smoothCamVelocity; // Pour un lissage plus stable en collision

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
        if (_isLocked) return; // Si la caméra est verrouillée, on ignore tout input de rotation
        HandleInput();
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        if (_playerController.LockOn != null && _playerController.LockOn.IsLocked)
        {
            UpdateLockOnRotation();
            return; // On ignore l'input de la souris/stick
        }
        Vector2 look = input.MouseLook + input.GamepadLook;
        yaw += look.x  * Time.deltaTime;
        pitch -= look.y * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }
    // La nouvelle méthode qui calcule la rotation vers l'ennemi
    private void UpdateLockOnRotation()
    {
        Transform targetEnemy = _playerController.LockOn.CurrentTarget;
        if (targetEnemy == null) return;

        // On calcule la direction entre la caméra et l'ennemi
        // Note : On vise souvent un peu au-dessus du pivot (la poitrine) pour un meilleur look
        //Vector3 targetPoint = targetEnemy.position + Vector3.up * 1.5f;
        Vector3 targetPoint;
        if (targetEnemy.TryGetComponent<EnemyControllerBase>(out var enemy))
        {
            targetPoint = targetEnemy.position + Vector3.up * enemy.enemyData.lockOnHeightOffset;
        }
        else
        {
            targetPoint = targetEnemy.position + Vector3.up * 0.5f; // Fallback
        }
        Vector3 dir = (targetPoint - transform.position).normalized;

        // On extrait le yaw et le pitch de cette direction
        Quaternion targetRot = Quaternion.LookRotation(dir);

        // On lisse la transition pour que la caméra ne "snappe" pas trop violemment
        float targetYaw = targetRot.eulerAngles.y;
        float targetPitch = targetRot.eulerAngles.x;

        // Attention : targetPitch peut être > 180, on doit le normaliser pour ton Clamp
        if (targetPitch > 180) targetPitch -= 360f;

        yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * smoothTime);
        pitch = Mathf.LerpAngle(pitch, targetPitch, Time.deltaTime * smoothTime);
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }
    private void UpdateCameraPosition()
    {
        // 1. Interpolation fluide des offsets (Visée/Idle)
        currentPivotOffset = Vector3.Lerp(currentPivotOffset, targetPivotOffset, Time.deltaTime * smoothTime);
        currentCamOffset = Vector3.Lerp(currentCamOffset, targetCamOffset, Time.deltaTime * smoothTime);
        _camComponent.fieldOfView = Mathf.Lerp(_camComponent.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

        // 2. Calcul des rotations
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Quaternion yawRot = Quaternion.Euler(0, yaw, 0);

        // 3. Calcul du pivot (où la caméra regarde)
        Vector3 pivotPos = target.position + yawRot * currentPivotOffset;

        // 4. GESTION DES COLLISIONS (Le cœur du système)
        float maxDistance = currentCamOffset.magnitude;
        Vector3 direction = rot * Vector3.back; // Direction vers l'arrière

        // On lance un rayon du pivot vers la position souhaitée de la caméra
        RaycastHit hit;
        float targetDistance = maxDistance;

        // On utilise SphereCast pour simuler le volume de la caméra
        if (Physics.SphereCast(pivotPos, cameraRadius, direction, out hit, maxDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            // On déduit une petite marge (cameraRadius) pour ne pas toucher le mur
            targetDistance = Mathf.Clamp(hit.distance, minCollisionDistance, maxDistance);
        }

        // Lissage de la distance (évite les sauts brusques quand on rase un mur)
        currentCollisionDistance = Mathf.Lerp(currentCollisionDistance, targetDistance, Time.deltaTime * collisionSmoothSpeed);

        // 5. Calcul de la position finale
        Vector3 finalPos = pivotPos + direction * currentCollisionDistance;

        // 6. Application
        transform.position = finalPos;

        // Optionnel : Un LookAt plus précis ou simplement utiliser la rotation rot
        transform.rotation = rot;
    }

    public Transform GetTransform() => transform;

    public void SetAimState(bool isAiming)
    {
        targetPivotOffset = isAiming ? aimPivotOffset : defaultPivotOffset;
        targetCamOffset = isAiming ? aimCamOffset : defaultCamOffset;
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
}

public static class CameraEvents
{
    public static Action<float, float> OnCameraShake;
    // amplitude, duration
}