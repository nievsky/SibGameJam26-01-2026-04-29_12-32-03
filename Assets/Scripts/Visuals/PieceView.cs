using UnityEngine;
using DG.Tweening; // Не забудь установить DOTween!

public class PieceView : MonoBehaviour
{
    public PieceType Type;
    public Alignment Alignment;
    public Vector2Int LogicPosition { get; set; }

    [Header("Animation Settings")]
    [SerializeField] private float _moveDuration = 0.4f;
    [SerializeField] private float _jumpHeight = 1f;

    public void MoveToWorldPosition(Vector3 targetWorldPos, System.Action onComplete = null)
    {
        // Используем DOJump для красивой параболической дуги при перемещении (имитация поднятия и опускания)
        transform.DOJump(targetWorldPos, _jumpHeight, 1, _moveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }
}