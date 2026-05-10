using UnityEngine;

public class Item : WorldDisappearOnCollected
{
    [Header("Item Data")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Visual Floating")]
    public bool enableFloating = false;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;


    [Header("Popup Event")]
    [SerializeField] private bool triggerPopupOnOpen = false;
    [SerializeField] private string popupMessage;


    private Vector3 startPosition;
    private float timeOffset;
    private Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 100f);

        if (rb != null)
        {
            rb.isKinematic = enableFloating;
            rb.useGravity = !enableFloating;
        }
    }

    private void Update()
    {
        if (!enableFloating)
            return;

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnDestroy()
    {
        //Debug.Log($"[Item] {name} destroyed. Triggering popup: {triggerPopupOnOpen}, Message: {popupMessage}");
        if (triggerPopupOnOpen)
            PopupEvent.Raise(popupMessage);
    }
}
