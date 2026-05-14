using System.Collections;
using UnityEngine;

public class BlockCamera : MonoBehaviour
{
    [Header("Camera Default Settings")]
    [SerializeField] private Vector2 defaultRotation = new Vector2(0f, 0f);

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // On accède directement à la caméra via son Instance
        var cam = ThirdPersonCameraController.Instance;

        if (cam == null) return;

        // On applique la rotation immédiatement
        cam.SetRotation(defaultRotation.x, defaultRotation.y);
        // On verrouille après un léger délai pour éviter les micro-saccades
        StartCoroutine(LockAfterDelay(cam, 0.05f));
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // On accède directement à la caméra via son Instance
        var cam = ThirdPersonCameraController.Instance;

        if (cam == null) return;

        cam.UnlockCamera();
    }

    private IEnumerator LockAfterDelay(ThirdPersonCameraController cam, float delay)
    {
        yield return new WaitForSeconds(delay);
        cam.LockCamera();
    }
} 