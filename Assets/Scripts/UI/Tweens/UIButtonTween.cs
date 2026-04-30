using UnityEngine;
using UnityEngine.EventSystems; // Needed for pointer events
using DG.Tweening;

public class UIButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;

    [Header("Animation Settings")]
    public float hoverScale = 1.1f;   // How big the button grows on hover
    public float animationDuration = 0.2f; // Duration of the tween

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }
    
    private void Start()
    {
        // Idle pulsing effect
        rectTransform.DOScale(originalScale * 1.02f, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo); // loops forever
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Kill any existing tweens to avoid conflicts
        rectTransform.DOKill();

        // Scale up then scale back (like a bounce)
        rectTransform.DOScale(originalScale * hoverScale, animationDuration)
            .SetEase(Ease.OutBack);

        UIAudioManager.Instance.PlayHoverSFX();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIAudioManager.Instance.PlayClickSFX();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Kill any existing tweens
        rectTransform.DOKill();

        // Scale back to original
        rectTransform.DOScale(originalScale, animationDuration)
            .SetEase(Ease.OutBack);
    }
}