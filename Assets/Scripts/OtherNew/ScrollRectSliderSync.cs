using UnityEngine;
using UnityEngine.UI;

public class ScrollRectSliderSync : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Slider slider;

    private bool _isSyncing = false;

    void Start()
    {
        if (scrollRect == null || slider == null) return;

        // On initialise la position
        slider.value = scrollRect.verticalNormalizedPosition;

        // On s'abonne aux changements
        slider.onValueChanged.AddListener(OnSliderChanged);
        scrollRect.onValueChanged.AddListener(OnScrollChanged);
    }

    void OnSliderChanged(float value)
    {
        if (_isSyncing) return;

        _isSyncing = true;
        // On met à jour le ScrollRect
        scrollRect.verticalNormalizedPosition = value;
        _isSyncing = false;
    }

    void OnScrollChanged(Vector2 value)
    {
        if (_isSyncing) return;

        _isSyncing = true;
        // On met à jour le Slider (on utilise value.y pour le vertical)
        slider.value = value.y;
        _isSyncing = false;
    }

    private void OnDestroy()
    {
        // Bonne pratique : on retire les écouteurs quand l'objet est détruit
        if (slider) slider.onValueChanged.RemoveListener(OnSliderChanged);
        if (scrollRect) scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
    }
}