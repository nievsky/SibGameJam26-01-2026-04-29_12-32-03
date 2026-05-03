using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public class CellView : MonoBehaviour
{
    public Vector2Int LogicPosition { get; private set; }
    
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    
    [Header("Текстуры состояний")]
    [SerializeField] private Texture2D _normalTexture;  // Назначается кодом (светлая/темная)
    [SerializeField] private Texture2D _inactiveTexture; // <-- НОВАЯ ТЕКСТУРА ДЛЯ ОТКЛЮЧЕННЫХ КЛЕТОК (Черная)
    [SerializeField] private Texture2D _moveTexture;
    [SerializeField] private Texture2D _attackTexture;
    [SerializeField] private Texture2D _threatTexture;
    [SerializeField] private Texture2D _hoverTexture;

    [SerializeField] private string _shaderTextureName = "_BaseMap"; // Для Standard шейдера поменяй на "_MainTex"

    public bool IsActiveCell { get; private set; }
    public bool IsThreatened { get; set; } = false;

    public void Init(Vector2Int logicPos, Texture2D baseTexture, bool isActive)
    {
        LogicPosition = logicPos;
        IsActiveCell = isActive;
        
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        _normalTexture = baseTexture; 
        
        // БОЛЬШЕ НЕ ВЫКЛЮЧАЕМ GAMEOBJECT!
        // Просто применяем правильную текстуру на старте
        ResetHighlight();
    }

    private void ApplyTexture(Texture2D tex)
    {
        if (tex == null) return;
        
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture(_shaderTextureName, tex); 
        _renderer.SetPropertyBlock(_propBlock);
    }

    // Обновленный метод для редактора
    public void SetActiveState(bool isActive, Texture2D baseTexture)
    {
        IsActiveCell = isActive;
        
        // Если передали шахматную текстуру (при включении) - запоминаем её
        if (baseTexture != null)
        {
            _normalTexture = baseTexture;
        }
        
        ResetHighlight(); 
    }

    public void HighlightAsMove() => ApplyTexture(_moveTexture);
    public void HighlightAsAttack() => ApplyTexture(_attackTexture);
    public void HighlightAsHover() => ApplyTexture(_hoverTexture);
    
    public void ResetHighlight()
    {
        if (IsThreatened)
        {
            ApplyTexture(_threatTexture);
        }
        else
        {
            // Если клетка активна - рисуем шахматный пол, если нет - черную текстуру
            ApplyTexture(IsActiveCell ? _normalTexture : _inactiveTexture);
        }
    }
}