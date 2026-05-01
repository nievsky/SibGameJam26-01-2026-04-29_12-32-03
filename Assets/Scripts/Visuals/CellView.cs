using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public class CellView : MonoBehaviour
{
    public Vector2Int LogicPosition { get; private set; }
    
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private Color _originalColor;

    // Настройки цветов подсветки
    [SerializeField] private Color _hoverColor = Color.yellow;
    [SerializeField] private Color _moveColor = Color.green;
    [SerializeField] private Color _attackColor = Color.red;

    public void Init(Vector2Int logicPos, Color baseColor)
    {
        LogicPosition = logicPos;
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        _originalColor = baseColor;
        SetColor(_originalColor);
    }

    private void SetColor(Color color)
    {
        // Запрашиваем текущий блок, меняем цвет и применяем обратно
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color); 
        _renderer.SetPropertyBlock(_propBlock);
    }

    // Методы для контроллера
    public void HighlightAsMove() => SetColor(_moveColor);
    public void HighlightAsAttack() => SetColor(_attackColor);
    public void HighlightAsHover() => SetColor(_hoverColor);
    public void ResetHighlight() => SetColor(_originalColor);
}
