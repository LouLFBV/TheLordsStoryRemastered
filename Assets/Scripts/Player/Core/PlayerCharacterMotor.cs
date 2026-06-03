using UnityEngine;

public class PlayerCharacterMotor : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    //[SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform cameraTransform;


    [Header("Roll Settings")]
    [SerializeField] private float rollHeightMultiplier = 0.5f;
    [SerializeField] private float rollCenterYOffset = -0.4f;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;
    private CapsuleCollider capsule;

    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial frictionMaterial;
    [SerializeField] private PhysicsMaterial slipperyMaterial;

    public void SetFriction(bool hasFriction)
    {
        // On change le matériau du collider selon le besoin
        capsule.material = hasFriction ? frictionMaterial : slipperyMaterial;
    }
    private void Awake()
    {
        capsule = player.GetComponent<CapsuleCollider>();
    }
    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = ThirdPersonCameraController.Instance.GetTransform();
        InitCollider(capsule);
    }
    public void RotateTowardsInput(Vector2 input)
    {
        if (input == Vector2.zero || player.interactSystem.isBusy)
            return;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.y = 0;

        Vector3 direction = forward * input.y + right * input.x;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        player.Rigidbody.MoveRotation(
            Quaternion.Slerp(
                player.Rigidbody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            )
        );
    }
    //public void Rotate(Vector2 input)
    //{
    //    // CAS 1 : On est verrouillé sur un ennemi
    //    if (player.LockOn != null && player.LockOn.IsLocked)
    //    {
    //        // On calcule la direction vers la cible
    //        Vector3 targetPos = player.LockOn.CurrentTarget.position;
    //        Vector3 dir = (targetPos - transform.position).normalized;
    //        dir.y = 0; // On reste bien vertical

    //        if (dir != Vector3.zero)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(dir);

    //            // On utilise MoveRotation pour que ce soit fluide et physique
    //            player.Rigidbody.MoveRotation(
    //                Quaternion.Slerp(
    //                    player.Rigidbody.rotation,
    //                    targetRot,
    //                    rotationSpeed * Time.fixedDeltaTime
    //                )
    //            );
    //        }
    //    }
    //    // CAS 2 : On n'est pas verrouillé, on tourne vers l'input
    //    else
    //    {
    //        // On appelle simplement ta méthode existante !
    //        RotateTowardsInput(input);
    //    }
    //}

    public bool IsGrounded()
    {
        Vector3 start = capsule.bounds.center;
        float radius = capsule.radius * 0.9f; 
        float rayLength = (capsule.height / 2f) - radius + 0.6f;

        if (Physics.SphereCast(start, radius, Vector3.down, out RaycastHit hit, rayLength, ~0, QueryTriggerInteraction.Ignore))
        {
            // On calcule l'angle
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);

            // Si la pente est trop raide, on renvoie false !
            // Ça va forcer le passage à l'état Fall
            if (slopeAngle > 60f) return false;

            return true;
        }
        return false;
    }
    public Vector3 GetDirectionFromInput(Vector2 input)
    {
        // 1. On récupère la direction avant de la caméra (Forward)
        Vector3 camForward = Camera.main.transform.forward;
        // 2. On récupère la direction droite de la caméra (Right)
        Vector3 camRight = Camera.main.transform.right;

        // 3. On ignore la composante Y (on ne veut pas que le perso s'enfonce dans le sol 
        // ou s'envole si la caméra regarde vers le bas/haut)
        camForward.y = 0;
        camRight.y = 0;

        // 4. On normalise pour garder une direction pure
        camForward.Normalize();
        camRight.Normalize();

        // 5. On combine avec l'input du joueur
        return (camForward * input.y + camRight * input.x).normalized;
    }


    public void InitCollider(CapsuleCollider col)
    {
        capsule = col;
        originalCapsuleHeight = capsule.height;
        originalCapsuleCenter = capsule.center;
    }

    public void StartRollCollider()
    {
        capsule.height = originalCapsuleHeight * rollHeightMultiplier;
        capsule.center = originalCapsuleCenter + Vector3.up * rollCenterYOffset;
    }

    public void EndRollCollider()
    {
        capsule.height = originalCapsuleHeight;
        capsule.center = originalCapsuleCenter;
    }
}