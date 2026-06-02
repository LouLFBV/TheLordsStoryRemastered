using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Mannequin : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject barreDeVie;
    [SerializeField] private Image vieImage;

    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth;

    [SerializeField] private Animator animator;

    [SerializeField] private float delayToRegen = 5f;

    private Coroutine regenCoroutine;
    void Start()
    {
        currentHealth = maxHealth;
        barreDeVie.SetActive(false);
    }


    public void TakeDamage(DamageInfo damageInfo)
    {
        barreDeVie.SetActive(true);

        currentHealth = Mathf.Max(currentHealth - damageInfo.rawPhysicalDamage, 0);
        animator.SetTrigger("SmallDamage");
        UpdateLife();


        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        regenCoroutine = StartCoroutine(RegenRoutine());
    }

    private void UpdateLife()
    {
        float healthRatio = currentHealth / maxHealth;
        vieImage.fillAmount = healthRatio;

        vieImage.color = Color.Lerp(Color.red, Color.yellow, healthRatio);
    }

    private IEnumerator RegenRoutine()
    {
        yield return new WaitForSeconds(delayToRegen);

        currentHealth = maxHealth;
        UpdateLife();


        yield return new WaitForSeconds(delayToRegen);
        barreDeVie.SetActive(false);
    }
}

