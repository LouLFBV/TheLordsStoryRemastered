using UnityEngine;

public class PopupParent : MonoBehaviour
{
    public static PopupParent Instance;

    public Transform parentPopup;
    public Transform parentItem;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
