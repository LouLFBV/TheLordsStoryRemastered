using UnityEngine; 

public class TransitionPanel : MonoBehaviour
{
    public static TransitionPanel Instance;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject iconeLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of TransitionPanel detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("TransitionPanel Awake");
    }
    private void OnDestroy()
    {
        Debug.Log("TransitionPanel Destroyed");
    }
    public void PlayTransitionIn()
    {
        if (animator == null)
        {
           animator = GetComponent<Animator>();
        }

        animator.SetTrigger("Open");
    }

    public void Continue()
    {
        animator.SetTrigger("Continue");
    }
    public void PlayTransitionOut()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        animator.SetTrigger("Close");
    }

    public void SetLoadingIconVisible(int visible)
    {
        animator.SetBool("Icone", visible == 1);
    }
}