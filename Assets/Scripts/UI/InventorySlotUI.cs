using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Обязательно для событий мыши
using DG.Tweening; // Обязательно для DOTween

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Данные слота")]
    public PieceType Type; // Какую фигуру представляет эта карточка
    
    [Header("UI Элементы")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Sprite _outlineSprite; // Спрайт-контур (заблокировано)
    [SerializeField] private Sprite _filledSprite;  // Цветной спрайт (разблокировано)
    
    [Header("DOTween Настройки")]
    [SerializeField] private float _hoverScale = 1.15f;
    [SerializeField] private float _hoverRotation = 10f;
    [SerializeField] private float _animDuration = 0.25f;

    private bool _isUnlocked = false;

    public void SetUnlocked(bool unlocked)
    {
        _isUnlocked = unlocked;
        _iconImage.sprite = unlocked ? _filledSprite : _outlineSprite;
        
        // Если вдруг мы сбросили инвентарь (рестарт), пока курсор был на карточке - жестко сбрасываем визуал
        if (!unlocked)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }
    }

    // Срабатывает, когда курсор ЗАХОДИТ на карточку
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Анимируем только если фигура есть в инвентаре
        if (!_isUnlocked) return; 

        transform.DOKill(); // Убиваем старые анимации (важно, если игрок быстро дергает мышкой)
        
        // Увеличиваем с эффектом "отскока" (OutBack) и немного поворачиваем
        transform.DOScale(_hoverScale, _animDuration).SetEase(Ease.OutBack);
        transform.DORotate(new Vector3(0, 0, _hoverRotation), _animDuration).SetEase(Ease.OutBack);
    }

    // Срабатывает, когда курсор УХОДИТ с карточки
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isUnlocked) return;

        transform.DOKill();
        
        // Плавно возвращаем в исходное состояние
        transform.DOScale(1f, _animDuration).SetEase(Ease.OutQuad);
        transform.DORotate(Vector3.zero, _animDuration).SetEase(Ease.OutQuad);
    }
}
