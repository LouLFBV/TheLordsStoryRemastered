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
    [SerializeField] private Quaternion initialFlecheRotation = new (330.536f, 204.401f, 331.288f,0f);

    public event Action<float> OnBowChargeProgress;
    public event Action<bool> OnBowChargeStateChanged;


    [SerializeField] private GameObject[] quiverArrows = new GameObject[10];

    [SerializeField] private bool changeLine;
    public BowstringBehaviour bowstringBehaviour;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bowChargeSound, bowShootSound;

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

        //weaponActive = Palette.instance.equipmentWeapon1Item ? Palette.instance.equipmentWeapon1Item : Palette.instance.equipmentWeapon2Item; ;
        weaponActive = PaletteSystem.instance.slotManager.weapons[0].itemData ? PaletteSystem.instance.slotManager.weapons[0].itemData : PaletteSystem.instance.slotManager.weapons[1].itemData; ;

        // On crée une rotation horizontale alignée avec l’arc
        Quaternion flatRotation = Quaternion.Euler(0f, 0f, 0f);

        //  On instancie directement sans parent
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

        animator.SetBool("ChargeBow", true);
        chargeBow = true;
        StartCoroutine(ChargingBow());
    }

    private IEnumerator ChargingBow()
    {
        if (bowstringBehaviour == null)
            yield break;
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
            float easedT = t * t; // ton easing actuel
            weaponActive.range = Mathf.Lerp(startPower, endPower, t);
            weaponActive.damage = Mathf.Lerp(weaponActive.damageMin, weaponActive.damageMax, t);

            charge01 = easedT;
            OnBowChargeProgress?.Invoke(charge01);

            yield return null;
        }

        weaponActive.range = endPower;
        weaponActive.damage = weaponActive.damageMax;
    }
    // Remplace la coroutine ChargingBow par ceci :
    public void UpdateChargeProgress(float t)
    {
        // t va de 0 à 1
        float easedT = Mathf.Pow(t, 4f); // Ton t * t * t * t (Mathf.Pow(t,2) * Mathf.Pow(t,2))

        weaponActive.range = Mathf.Lerp(weaponActive.rangeMin, weaponActive.rangeMax, t);
        weaponActive.damage = Mathf.Lerp(weaponActive.damageMin, weaponActive.damageMax, t);

        OnBowChargeProgress?.Invoke(easedT);
    }

    public void ShootArrow()
    {
        Debug.Log("Lacher la fleche");
        arrow.transform.parent = null;
        arrow.GetComponent<Rigidbody>().isKinematic = false;
        arrow.GetComponent<BoxCollider>().enabled = true;
        arrow.GetComponent<WeaponDamageDetector>().itemData.attackPoints = weaponActive.damage;

        AlignArrowSpawnToCamera();
        audioSource.PlayOneShot(bowShootSound);
        arrow.GetComponent<Rigidbody>().AddForce(arrowSpawnPoint.forward * weaponActive.range, ForceMode.Impulse);
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
        //  SÉCURITÉ 1 : Si playerCamera est null (ce qui arrive quand on charge le préfab),
        // on récupère automatiquement la caméra principale du jeu.
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogError("[BowBehaviour] Impossible de tirer : Aucune caméra principale trouvée dans la scène !");
            return;
        }

        // Ray depuis le centre de l’écran
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int layerMask = ~LayerMask.GetMask("Player");

        Vector3 targetPoint;

        //  SÉCURITÉ 2 : On ajoute 'QueryTriggerInteraction.Ignore' à la fin du Raycast.
        // Cela empêche le tir de s'aligner sur des colliders invisibles (triggers de zone, etc.)
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask, QueryTriggerInteraction.Ignore))
        {
            //  SÉCURITÉ 3 : Si le Raycast touche un objet trop proche du joueur (comme l'arc lui-même ou un obstacle au corps à corps)
            // on force la cible à être loin devant pour éviter que l'arc ne louche vers le sol.
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

        // Oriente le spawn vers ce point
        arrowSpawnPoint.LookAt(targetPoint);

        // (debug visuel)
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

        // Mets à jour la forme de la corde
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