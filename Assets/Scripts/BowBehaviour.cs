using System;
using System.Collections;
using UnityEngine;

public class BowBehaviour : MonoBehaviour
{
    public static BowBehaviour instance;
    [SerializeField] private GameObject arrowPrefab, fireArrowPrefab, electricArrowPrefab, iceArrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    [SerializeField] private Animator animator;
    [SerializeField] private Camera playerCamera;

    public bool canShoot = false;
    public bool chargeBow;
    private float charge01;
    private GameObject arrow;
    private ItemData weaponActive;
    private ItemInInventory arrowItem;
    [SerializeField] private Quaternion initialFlecheRotation = new(330.536f, 204.401f, 331.288f, 0f);

    public event Action<float> OnBowChargeProgress;
    public event Action<bool> OnBowChargeStateChanged;

    [SerializeField] private GameObject[] quiverArrows = new GameObject[10];

    [SerializeField] private bool changeLine;
    public BowstringBehaviour bowstringBehaviour;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bowChargeSound, bowShootSound;

    private float currentArrowForce;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (bowstringBehaviour != null)
            UpdateStringVisual();
    }

    public bool VerifIfCanShoot()
    {
        arrowItem = EquipmentSystem.instance.arrowItemInInventory;
        return arrowItem != null && arrowItem.count > 0;
    }

    public void PrepareArrow()
    {
        Debug.Log("Tirer une fleche");

        weaponActive = PaletteSystem.instance.slotManager.weapons[0].itemData ? PaletteSystem.instance.slotManager.weapons[0].itemData : PaletteSystem.instance.slotManager.weapons[1].itemData;

        Quaternion flatRotation = Quaternion.Euler(0f, 0f, 0f);

        switch (EquipmentSystem.instance.arrowItemInInventory.itemData.effet)
        {
            case Effet.Feu:
                arrow = Instantiate(fireArrowPrefab, arrowSpawnPoint.position, flatRotation, arrowSpawnPoint);
                break;
            case Effet.Foudre:
                arrow = Instantiate(electricArrowPrefab, arrowSpawnPoint.position, flatRotation, arrowSpawnPoint);
                break;
            case Effet.Glace:
                arrow = Instantiate(iceArrowPrefab, arrowSpawnPoint.position, flatRotation, arrowSpawnPoint);
                break;
            default:
                arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, flatRotation, arrowSpawnPoint);
                break;
        }

        arrow.SetActive(false);
        arrow.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        BoxCollider col = arrow.GetComponent<BoxCollider>();
        col.enabled = false;

        currentArrowForce = weaponActive.rangeMin;

        animator.SetBool("ChargeBow", true);
        chargeBow = true;

        StartCoroutine(ChargingBow()); 
    }

    private IEnumerator ChargingBow()
    {
        if (bowstringBehaviour == null) yield break;
        float chargeDuration = 2.067f;
        float currentChargeTime = 0f;

        float startPower = weaponActive.rangeMin;
        float endPower = weaponActive.rangeMax;

        OnBowChargeStateChanged?.Invoke(true);
        audioSource.PlayOneShot(bowChargeSound);

        while (chargeBow && currentChargeTime < chargeDuration)
        {
            currentChargeTime += Time.deltaTime;
            float t = Mathf.Pow(currentChargeTime / chargeDuration, 2f);
            float easedT = t * t;

            currentArrowForce = Mathf.Lerp(startPower, endPower, t);

            charge01 = easedT;
            OnBowChargeProgress?.Invoke(charge01);

            yield return null;
        }

        currentArrowForce = endPower;
    }

    // Appelée par ton PlayerBowChargeState
    public void UpdateChargeProgress(float t)
    {
        float easedT = Mathf.Pow(t, 4f);

        currentArrowForce = Mathf.Lerp(weaponActive.rangeMin, weaponActive.rangeMax, t);

        OnBowChargeProgress?.Invoke(easedT);
    }

    public void ShootArrow()
    {
        Debug.Log("Lacher la fleche");

        AlignArrowSpawnToCamera();

        arrow.transform.rotation = arrowSpawnPoint.rotation;
        arrow.transform.parent = null;

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        arrow.GetComponent<BoxCollider>().enabled = true;

        audioSource.PlayOneShot(bowShootSound);

        rb.AddForce(arrowSpawnPoint.forward * currentArrowForce, ForceMode.Impulse);

        chargeBow = false;
        OnBowChargeStateChanged?.Invoke(false);
        OnBowChargeProgress?.Invoke(0f);
        changeLine = false;

        animator.SetBool("ChargeBow", false);
        animator.SetTrigger("ArrowShoot");
        Destroy(arrow, 5f);

        arrowSpawnPoint.localRotation = initialFlecheRotation;
    }

    private void AlignArrowSpawnToCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogError("[BowBehaviour] Impossible de tirer : Aucune caméra principale trouvée dans la scène !");
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int layerMask = ~LayerMask.GetMask("Player");
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Distance(arrowSpawnPoint.position, hit.point) < 2f)
            {
                targetPoint = ray.origin + ray.direction * 100f;
            }
            else
            {
                targetPoint = hit.point;
            }
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 100f;
        }

        arrowSpawnPoint.LookAt(targetPoint);
        Debug.DrawLine(arrowSpawnPoint.position, targetPoint, Color.yellow, 0.5f);
    }

    private void UpdateStringVisual()
    {
        if (bowstringBehaviour.line == null || bowstringBehaviour.bowTop == null || bowstringBehaviour.bowBottom == null) return;

        Vector3 middlePoint;

        if (changeLine && bowstringBehaviour.stringHandPoint != null)
        {
            middlePoint = bowstringBehaviour.stringHandPoint.position;
        }
        else
        {
            middlePoint = bowstringBehaviour.bowTop.position;
        }

        bowstringBehaviour.line.SetPosition(0, bowstringBehaviour.bowTop.position);
        bowstringBehaviour.line.SetPosition(1, middlePoint);
        bowstringBehaviour.line.SetPosition(2, bowstringBehaviour.bowBottom.position);
    }

    public void ActiveChangeLine() => changeLine = true;

    public void UpdateQuiverVisual(int currentArrowCount)
    {
        for (int i = 0; i < quiverArrows.Length; i++)
        {
            quiverArrows[i].SetActive(i < currentArrowCount);
        }
        EquipmentSystem.instance.UpdateQuiverVisual(currentArrowCount);
    }

    public void ActiveArrow()
    {
        arrow.SetActive(true);
        arrowItem.count--;
        PaletteSystem.instance.slotManager.UpdateCountArrow(arrowItem.count);
        EquipmentSystem.instance.UpdateArrowsText();
        if (arrowItem.count == 0)
            EquipmentSystem.instance.DesequipEquipment(arrowItem.itemData.equipmentType);
        UpdateQuiverVisual(arrowItem.count);
    }

    public void ActiveCanShoot() { canShoot = true; }
    public void DesactiveCanShoot() { canShoot = false; }
}