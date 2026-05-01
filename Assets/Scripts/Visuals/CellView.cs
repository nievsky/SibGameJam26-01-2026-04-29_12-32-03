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
    [SerializeField] private Color _inactiveColor = Color.black; // Цвет отключенной клетки
    public bool IsActiveCell { get; private set; }
    
    [SerializeField] private Color _threatColor = new Color(0.8f, 0.3f, 0.1f); // Темно-оранжевый/красный
    public bool IsThreatened { get; set; } = false;

    public void Init(Vector2Int logicPos, Color baseColor, bool isActive)
    {
        LogicPosition = logicPos;
        IsActiveCell = isActive;
        
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        // Если клетка активна - шахматный цвет, если нет - цвет неактивной
        _originalColor = isActive ? baseColor : _inactiveColor;
        SetColor(_originalColor);
    }

    private void SetColor(Color color)
    {
        // Запрашиваем текущий блок, меняем цвет и применяем обратно
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color); 
        _renderer.SetPropertyBlock(_propBlock);
    }
    
    // Добавим метод для быстрого переключения состояния из редактора
    public void SetActiveState(bool isActive, Color baseColor)
    {
        IsActiveCell = isActive;
        _originalColor = isActive ? baseColor : _inactiveColor;
        ResetHighlight(); // Применяем цвет
    }

    // Методы для контроллера
    public void HighlightAsMove() => SetColor(_moveColor);
    public void HighlightAsAttack() => SetColor(_attackColor);
    public void HighlightAsHover() => SetColor(_hoverColor);
    public void ResetHighlight()
    {
        // Если клетка под ударом, базовым цветом становится цвет угрозы
        SetColor(IsThreatened ? _threatColor : _originalColor);
    }
}
