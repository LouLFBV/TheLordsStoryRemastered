using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IconeUI : MonoBehaviour
{
    [SerializeField] private string actionName = "Interact";
    private Image icone;
    private DeviceType currentDevice;

    private Vector3 baseScale;
    private Coroutine pulseRoutine;

    [Header("Animation")]
    public float pulseSpeed = 1.5f;      // vitesse du pulse
    public float pulseScale = 1.15f;     // taille max du pulse

    private bool isForcedDevice = false;
    private DeviceType forcedDevice;

    private void Awake()
    {
        icone = GetComponent<Image>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(InitializeDeviceWatcher());
        StartCoroutine(UpdateIconWhenReady());

        // démarrer la pulsation
        pulseRoutine = StartCoroutine(PulseIcon());
    }

    private void OnDisable()
    {
        if (DeviceWatcher.Instance != null)
            DeviceWatcher.Instance.OnDeviceChanged -= UpdateDevice;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        transform.localScale = baseScale;
    }

    public void SetActionAndDevice(string newActionName, bool forceDevice = false, DeviceType device = default)
    {
        actionName = newActionName;
        isForcedDevice = forceDevice;

        if (forceDevice)
        {
            forcedDevice = device;
            currentDevice = device;
        }

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(UpdateIconWhenReady());
        }
    }

    private IEnumerator UpdateIconWhenReady()
    {
        while (InputProvider.Instance == null)
            yield return null;

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (icone == null || InputProvider.Instance == null)
            return;

        InputRebindManager.UpdateBindingDisplayForAction(
            InputProvider.Instance.UIInput.actions[actionName],
            icone,
            currentDevice
        );
    }

    private IEnumerator InitializeDeviceWatcher()
    {
        while (DeviceWatcher.Instance == null)
            yield return null;

        DeviceWatcher.Instance.OnDeviceChanged += UpdateDevice;

        currentDevice = isForcedDevice ? forcedDevice : DeviceWatcher.Instance.CurrentDevice;
        UpdateIcon();
    }

    private void UpdateDevice(DeviceType device)
    {

        if (isForcedDevice) return;

        currentDevice = device;
        StartCoroutine(UpdateIconWhenReady());
    }

    // --- Animation de pulsation ---

    private IEnumerator PulseIcon()
    {
        while (true)
        {
            // monte
            yield return ScaleTo(pulseScale);

            // redescend
            yield return ScaleTo(1f);
        }
    }

    private IEnumerator ScaleTo(float targetScale)
    {
        Vector3 start = transform.localScale;
        Vector3 end = baseScale * targetScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * pulseSpeed; // unscaled = animation continue même en pause
            transform.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
}