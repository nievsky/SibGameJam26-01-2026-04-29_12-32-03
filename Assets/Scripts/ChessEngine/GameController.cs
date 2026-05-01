using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Prefabs & Setup")]
    [SerializeField] private CellView _cellPrefab;
    [SerializeField] private PieceView _knightPrefab; // Для примера возьмем префаб Коня
    [SerializeField] private PieceView _enemyRookPrefab;
    [SerializeField] private float _cellSize = 1.1f;  // Расстояние между 3D клетками
    [SerializeField] private LayerMask _cellLayer;    // Слой, на котором находятся клетки (для Raycast)

    private ChessEngine _engine;
    private Dictionary<Vector2Int, CellView> _cellViews = new Dictionary<Vector2Int, CellView>();
    private Dictionary<Vector2Int, PieceView> _pieceViews = new Dictionary<Vector2Int, PieceView>();

    // Состояние инпута
    private PieceView _selectedPiece = null;
    private List<Vector2Int> _currentValidMoves = new List<Vector2Int>();
    private bool _isAnimating = false;
    
    private CellView _currentHoveredCell = null;

    private void Start()
    {
        GenerateBoard(8, 8);
        SpawnPlayerPiece(new Vector2Int(0, 0));
        
        // Спавним тестового врага на (3, 3)
        SpawnEnemyPiece(new Vector2Int(3, 3), PieceType.Rook, _enemyRookPrefab);

        // Считаем угрозы в самом начале
        _engine.UpdateThreatMap();
        RefreshBoardThreats();
    }

    private void GenerateBoard(int width, int height)
    {
        _engine = new ChessEngine(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Вычисляем позицию в 3D пространстве
                Vector3 worldPos = new Vector3(x * _cellSize, 0, y * _cellSize);
                
                // Создаем визуал
                CellView cellView = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
                cellView.name = $"Cell {x}_{y}";
                
                // Делаем доску "шахматной" по цветах для красоты
                Color baseColor = (x + y) % 2 == 0 ? Color.white : Color.gray;
                cellView.Init(new Vector2Int(x, y), baseColor);

                _cellViews.Add(new Vector2Int(x, y), cellView);
            }
        }
    }

    private void SpawnPlayerPiece(Vector2Int logicPos)
    {
        // 1. Обновляем логику
        BoardCell cell = _engine.GetCell(logicPos);
        cell.CurrentPiece = PieceType.Knight;
        cell.PieceAlignment = Alignment.Player;

        // 2. Обновляем визуал
        Vector3 worldPos = _cellViews[logicPos].transform.position;
        worldPos.y += 0.5f; // Поднимаем над клеткой
        
        PieceView pieceView = Instantiate(_knightPrefab, worldPos, Quaternion.identity);
        pieceView.LogicPosition = logicPos;
        pieceView.Type = PieceType.Knight;
        pieceView.Alignment = Alignment.Player;

        _pieceViews.Add(logicPos, pieceView);
    }
    
    private void SpawnEnemyPiece(Vector2Int logicPos, PieceType type, PieceView prefab)
    {
        BoardCell cell = _engine.GetCell(logicPos);
        cell.CurrentPiece = type;
        cell.PieceAlignment = Alignment.Enemy;

        Vector3 worldPos = _cellViews[logicPos].transform.position;
        worldPos.y += 0.5f;
        
        PieceView pieceView = Instantiate(prefab, worldPos, Quaternion.identity);
        pieceView.LogicPosition = logicPos;
        pieceView.Type = type;
        pieceView.Alignment = Alignment.Enemy;

        _pieceViews.Add(logicPos, pieceView);
    }

    private void Update()
    {
        if (_isAnimating) return; // Блокируем инпут во время анимации хода
        HandleHover();      // Сначала обрабатываем наведение
        HandleMouseInput();
    }

    private void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Пытаемся получить клетку (напрямую или через фигуру, стоящую на ней)
            CellView targetCell = null;

            if (hit.collider.TryGetComponent(out CellView hitCell))
            {
                targetCell = hitCell;
            }
            else if (hit.collider.TryGetComponent(out PieceView hitPiece))
            {
                targetCell = _cellViews[hitPiece.LogicPosition];
            }

            // Если мы навелись на новую клетку
            if (targetCell != null && targetCell != _currentHoveredCell)
            {
                ClearHover(); // Очищаем старую
                _currentHoveredCell = targetCell;

                // Подсвечиваем как Hover ТОЛЬКО если эта клетка сейчас не подсвечена как валидный ход/атака
                if (!_currentValidMoves.Contains(_currentHoveredCell.LogicPosition))
                {
                    _currentHoveredCell.HighlightAsHover();
                }
            }
        }
        else
        {
            ClearHover(); // Мышь ушла с доски
        }
    }

    private void ClearHover()
    {
        if (_currentHoveredCell != null)
        {
            // Восстанавливаем цвет. 
            // Если клетка является доступным ходом, возвращаем ей цвет хода/атаки.
            // Если нет — сбрасываем до оригинального (белый/серый).
            if (_currentValidMoves.Contains(_currentHoveredCell.LogicPosition))
            {
                if (_engine.GetCell(_currentHoveredCell.LogicPosition).HasEnemy)
                    _currentHoveredCell.HighlightAsAttack();
                else
                    _currentHoveredCell.HighlightAsMove();
            }
            else
            {
                _currentHoveredCell.ResetHighlight();
            }
        
            _currentHoveredCell = null;
        }
    }
    
    private void HandleMouseInput()
{
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector2Int? targetLogicPos = null;

            // 1. Проверяем, попали ли мы по клетке
            if (hit.collider.TryGetComponent(out CellView clickedCell))
            {
                targetLogicPos = clickedCell.LogicPosition;
            }
            // 2. ИЛИ проверяем, попали ли мы по фигуре
            else if (hit.collider.TryGetComponent(out PieceView clickedPiece))
            {
                targetLogicPos = clickedPiece.LogicPosition;

                // Если кликнули по своей фигуре — выделяем её и прерываем логику
                if (clickedPiece.Alignment == Alignment.Player)
                {
                    SelectPiece(targetLogicPos.Value);
                    return; 
                }
            }

            // 3. Обрабатываем клик по целевой позиции (неважно, кликнули по самой клетке или по врагу на ней)
            if (targetLogicPos.HasValue)
            {
                Vector2Int pos = targetLogicPos.Value;
                BoardCell logicCell = _engine.GetCell(pos);

                // Если фигура УЖЕ выбрана и мы кликаем по доступной клетке (даже с врагом) — делаем ход (рубим)
                if (_selectedPiece != null && _currentValidMoves.Contains(pos))
                {
                    ExecuteMove(_selectedPiece.LogicPosition, pos);
                }
                // На случай, если игрок кликнул именно по клетке под своей фигурой
                else if (logicCell.HasPlayer)
                {
                    SelectPiece(pos);
                }
                else
                {
                    DeselectPiece();
                }
            }
            else
            {
                DeselectPiece(); // Попали во что-то левое (например, фон)
            }
        }
        else
        {
            DeselectPiece(); // Клик мимо всего
        }
    }
}

    private void OnCellClicked(Vector2Int clickedPos)
    {
        BoardCell logicCell = _engine.GetCell(clickedPos);

        // 1. Если кликнули по своей фигуре — выделяем её
        if (logicCell.HasPlayer)
        {
            SelectPiece(clickedPos);
            return;
        }

        // 2. Если у нас выбрана фигура и мы кликнули по доступной для хода клетке — идем
        if (_selectedPiece != null && _currentValidMoves.Contains(clickedPos))
        {
            ExecuteMove(_selectedPiece.LogicPosition, clickedPos);
        }
    }

    private void SelectPiece(Vector2Int piecePos)
    {
        DeselectPiece(); // Сбрасываем предыдущие подсветки

        _selectedPiece = _pieceViews[piecePos];
        _currentValidMoves = _engine.GetValidMoves(piecePos);

        // Подсвечиваем доступные клетки
        foreach (Vector2Int movePos in _currentValidMoves)
        {
            if (_engine.GetCell(movePos).HasEnemy)
                _cellViews[movePos].HighlightAsAttack();
            else
                _cellViews[movePos].HighlightAsMove();
        }
    }

    private void DeselectPiece()
    {
        _selectedPiece = null;
        foreach (Vector2Int movePos in _currentValidMoves)
        {
            _cellViews[movePos].ResetHighlight();
        }
        _currentValidMoves.Clear();
    }

    private void ExecuteMove(Vector2Int fromPos, Vector2Int toPos)
    {
        DeselectPiece();
        _isAnimating = true;

        // --- ЛОГИКА РУБКИ ВРАГА ---
        if (_pieceViews.TryGetValue(toPos, out PieceView enemyPiece))
        {
            if (enemyPiece.Alignment == Alignment.Enemy)
            {
                // Уничтожаем 3D объект врага (здесь можно добавить партиклы взрыва)
                Destroy(enemyPiece.gameObject);
                _pieceViews.Remove(toPos);
            }
        }

        // 1. Двигаем в логике движка
        _engine.MovePiece(fromPos, toPos);
        
        // 2. Сразу пересчитываем карту угроз (так как мы могли убить врага или перекрыть линию)
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        // 3. Обновляем словари визуала
        PieceView movingPiece = _pieceViews[fromPos];
        _pieceViews.Remove(fromPos);
        _pieceViews[toPos] = movingPiece;
        movingPiece.LogicPosition = toPos;

        // 4. Анимируем визуал
        Vector3 targetWorldPos = _cellViews[toPos].transform.position;
        targetWorldPos.y += 0.5f;

        movingPiece.MoveToWorldPosition(targetWorldPos, () => 
        {
            _isAnimating = false; 

            // --- ЛОГИКА СМЕРТИ ИГРОКА ---
            if (_engine.GetCell(toPos).IsUnderEnemyAttack)
            {
                Debug.Log("And he sacrificed THE RULES!!!");
                _engine.GetCell(toPos).ClearPiece(); // Очищаем логику
                _pieceViews.Remove(toPos);
                Destroy(movingPiece.gameObject);     // Уничтожаем 3D модель игрока
                
                // Здесь можно вызвать логику проигрыша или перезапуска уровня
            }
        });
    }
    
    private void RefreshBoardThreats()
    {
        foreach (var kvp in _cellViews)
        {
            Vector2Int pos = kvp.Key;
            CellView view = kvp.Value;
            
            view.IsThreatened = _engine.GetCell(pos).IsUnderEnemyAttack;
            view.ResetHighlight(); // Перекрасит клетку в _threatColor, если IsThreatened == true
        }
    }
}