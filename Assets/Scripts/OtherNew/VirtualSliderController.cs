using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualSliderController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Slider _slider;
    private bool _isHovered = false;

    [SerializeField] private float _sensitivity = 2f;

    void Awake() => _slider = GetComponent<Slider>();

    public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => _isHovered = false;

    void Update()
    {
        // Si le curseur virtuel survole ce slider, on écoute le stick
        if (_isHovered)
        {
            float inputX = PlayerController.Instance.Input.NavigateLook.x;
            if (Mathf.Abs(inputX) > 0.1f)
            {
                // On ajuste la valeur directement
                _slider.value += inputX * Time.unscaledDeltaTime * _sensitivity;
            }
        }
    }
}