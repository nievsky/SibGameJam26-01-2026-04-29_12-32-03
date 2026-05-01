using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // Нужно для сохранения ScriptableObject
#endif

public class GameController : MonoBehaviour
{
    [Header("Prefabs & Setup")]
    [SerializeField] private CellView _cellPrefab;
    [SerializeField] private float _cellSize = 1.1f;  // Расстояние между 3D клетками
    [SerializeField] private LayerMask _cellLayer; 
    
    [Header("Уровень")]
    [SerializeField] private LevelData _currentLevel; // Сюда перетянешь файл Level_1

    [Header("Префабы Фигур (Игрок)")]
    [SerializeField] private PieceView _playerKnight;
    [SerializeField] private PieceView _playerQueen;
    [SerializeField] private PieceView _playerBishop;
    [SerializeField] private PieceView _playerKing;
    [SerializeField] private PieceView _playerRook;
    [SerializeField] private PieceView _playerPawn;
    // Добавь сюда остальные префабы игрока: _playerRook, _playerPawn и т.д.

    [Header("Префабы Фигур (Враг)")]
    [SerializeField] private PieceView _enemyRook;
    [SerializeField] private PieceView _enemyPawn;// Слой, на котором находятся клетки (для Raycast)
    [SerializeField] private PieceView _enemyKnight;
    [SerializeField] private PieceView _enemyBishop;
    [SerializeField] private PieceView _enemyQueen;
    [SerializeField] private PieceView _enemyKing;
    
    [Header("Редактор Уровней")]
    public bool IsEditMode = false;

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
        if (_currentLevel != null)
        {
            LoadLevel(_currentLevel);
        }
        else
        {
            Debug.LogError("Уровень не назначен!");
        }
    }
    
    private void LoadLevel(LevelData levelData)
    {
        _engine = new ChessEngine(levelData.Width, levelData.Height);

        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                CellSetup setup = levelData.Rows[y].Columns[x];
                
                // 1. Настраиваем логику клетки (активна ли она)
                _engine.GetCell(x, y).IsActive = setup.IsActive;

                // 2. Спавним 3D визуал клетки (если она не активна, можем вообще не спавнить или спавнить другой префаб стены)
                Vector3 cellPos = new Vector3(x * _cellSize, 0, y * _cellSize);
                CellView cellView = Instantiate(_cellPrefab, cellPos, Quaternion.identity, transform);
                cellView.name = $"Cell {x}_{y}";
    
                Color baseColor = (x + y) % 2 == 0 ? Color.white : Color.gray;
                    // ТЕПЕРЬ ПЕРЕДАЕМ setup.IsActive
                cellView.Init(new Vector2Int(x, y), baseColor, setup.IsActive); 
    
                _cellViews.Add(new Vector2Int(x, y), cellView);
                

                // 3. Спавним фигуры, если они указаны в конструкторе
                if (setup.Piece != PieceType.None && setup.Alignment != Alignment.None)
                {
                    SpawnPieceFromSetup(new Vector2Int(x, y), setup);
                }
            }
        }

        // Обновляем угрозы после расстановки всех фигур
        _engine.UpdateThreatMap();
        RefreshBoardThreats();
    }
    
    private void SpawnPieceFromSetup(Vector2Int logicPos, CellSetup setup)
    {
        // 1. Записываем в логику
        BoardCell cell = _engine.GetCell(logicPos);
        cell.CurrentPiece = setup.Piece;
        cell.PieceAlignment = setup.Alignment;

        // 2. Выбираем правильный префаб
        PieceView prefabToSpawn = GetPrefab(setup.Piece, setup.Alignment);
        if (prefabToSpawn == null) return;

        // 3. Спавним 3D
        Vector3 worldPos = _cellViews[logicPos].transform.position;
        worldPos.y += 0.5f;
        
        PieceView pieceView = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
        pieceView.LogicPosition = logicPos;
        pieceView.Type = setup.Piece;
        pieceView.Alignment = setup.Alignment;

        _pieceViews.Add(logicPos, pieceView);
    }

    // Вспомогательный метод для выбора нужного префаба
    private PieceView GetPrefab(PieceType type, Alignment alignment)
    {
        if (alignment == Alignment.Player)
        {
            switch (type)
            {
                case PieceType.Knight: return _playerKnight;
                // case PieceType.Rook: return _playerRook; 
                // добавь свои префабы
            }
        }
        else if (alignment == Alignment.Enemy)
        {
            switch (type)
            {
                case PieceType.Rook: return _enemyRook;
                case PieceType.Pawn: return _enemyPawn;
                // добавь свои префабы
            }
        }
        return null;
    }

    // private void GenerateBoard(int width, int height)
    // {
    //     _engine = new ChessEngine(width, height);
    //
    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int y = 0; y < height; y++)
    //         {
    //             // Вычисляем позицию в 3D пространстве
    //             Vector3 worldPos = new Vector3(x * _cellSize, 0, y * _cellSize);
    //             
    //             // Создаем визуал
    //             CellView cellView = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
    //             cellView.name = $"Cell {x}_{y}";
    //             
    //             // Делаем доску "шахматной" по цветах для красоты
    //             Color baseColor = (x + y) % 2 == 0 ? Color.white : Color.gray;
    //             cellView.Init(new Vector2Int(x, y), baseColor);
    //
    //             _cellViews.Add(new Vector2Int(x, y), cellView);
    //         }
    //     }
    // }

    // private void SpawnPlayerPiece(Vector2Int logicPos)
    // {
    //     // 1. Обновляем логику
    //     BoardCell cell = _engine.GetCell(logicPos);
    //     cell.CurrentPiece = PieceType.Knight;
    //     cell.PieceAlignment = Alignment.Player;
    //
    //     // 2. Обновляем визуал
    //     Vector3 worldPos = _cellViews[logicPos].transform.position;
    //     worldPos.y += 0.5f; // Поднимаем над клеткой
    //     
    //     PieceView pieceView = Instantiate(_knightPrefab, worldPos, Quaternion.identity);
    //     pieceView.LogicPosition = logicPos;
    //     pieceView.Type = PieceType.Knight;
    //     pieceView.Alignment = Alignment.Player;
    //
    //     _pieceViews.Add(logicPos, pieceView);
    // }
    
    // private void SpawnEnemyPiece(Vector2Int logicPos, PieceType type, PieceView prefab)
    // {
    //     BoardCell cell = _engine.GetCell(logicPos);
    //     cell.CurrentPiece = type;
    //     cell.PieceAlignment = Alignment.Enemy;
    //
    //     Vector3 worldPos = _cellViews[logicPos].transform.position;
    //     worldPos.y += 0.5f;
    //     
    //     PieceView pieceView = Instantiate(prefab, worldPos, Quaternion.identity);
    //     pieceView.LogicPosition = logicPos;
    //     pieceView.Type = type;
    //     pieceView.Alignment = Alignment.Enemy;
    //
    //     _pieceViews.Add(logicPos, pieceView);
    // }

    private void Update()
    {
        // Включение/выключение режима редактора на клавишу TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            IsEditMode = !IsEditMode;
            Debug.Log(IsEditMode ? "РЕЖИМ РЕДАКТОРА ВКЛЮЧЕН" : "РЕЖИМ РЕДАКТОРА ВЫКЛЮЧЕН");
            DeselectPiece();
        }

        if (_isAnimating) return;

        if (IsEditMode)
        {
            HandleEditorInput(); // Если редактор включен, перехватываем клики
        }
        else
        {
            HandleHover();      
            HandleMouseInput(); 
        }
    }

    // НОВЫЙ МЕТОД: Логика кликов в режиме редактора
    private void HandleEditorInput()
    {
        if (Input.GetMouseButtonDown(0)) // Левый клик мыши
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider.TryGetComponent(out CellView clickedCell))
                {
                    ToggleCellState(clickedCell.LogicPosition);
                }
            }
        }
    }

    // НОВЫЙ МЕТОД: Переключение клетки и сохранение в файл
    private void ToggleCellState(Vector2Int pos)
    {
        // 1. Получаем текущие данные из загруженного файла
        CellSetup setup = _currentLevel.Rows[pos.y].Columns[pos.x];
        
        // 2. Инвертируем состояние
        setup.IsActive = !setup.IsActive;

        // 3. Обновляем логику движка
        _engine.GetCell(pos).IsActive = setup.IsActive;

        // 4. Обновляем визуал на сцене
        Color baseColor = (pos.x + pos.y) % 2 == 0 ? Color.white : Color.gray;
        _cellViews[pos].SetActiveState(setup.IsActive, baseColor);

        // 5. Пересчитываем угрозы (если мы убрали или поставили стену, линии атаки могли измениться)
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        // 6. СОХРАНЯЕМ ФАЙЛ SCRIPTABLE OBJECT НА ДИСК
        #if UNITY_EDITOR
        EditorUtility.SetDirty(_currentLevel); // Помечаем файл как измененный
        AssetDatabase.SaveAssets();            // Принудительно сохраняем изменения на диск
        #endif

        Debug.Log($"Клетка {pos} теперь {(setup.IsActive ? "Активна" : "Стена")}. Файл сохранен.");
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

        // --- ЛОГИКА РУБКИ ВРАГА ИГРОКОМ ---
        if (_pieceViews.TryGetValue(toPos, out PieceView enemyPiece))
        {
            if (enemyPiece.Alignment == Alignment.Enemy)
            {
                Destroy(enemyPiece.gameObject);
                _pieceViews.Remove(toPos);
            }
        }

        // 1. Двигаем игрока в логике движка
        _engine.MovePiece(fromPos, toPos);
    
        // 2. Сразу пересчитываем угрозы (теперь враги видят игрока на новой позиции)
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        // 3. Обновляем словари визуала
        PieceView movingPiece = _pieceViews[fromPos];
        _pieceViews.Remove(fromPos);
        _pieceViews[toPos] = movingPiece;
        movingPiece.LogicPosition = toPos;

        // 4. Анимируем прыжок ИГРОКА
        Vector3 targetWorldPos = _cellViews[toPos].transform.position;
        targetWorldPos.y += 0.5f;

        movingPiece.MoveToWorldPosition(targetWorldPos, () => 
        {
            BoardCell currentCell = _engine.GetCell(toPos);

            // --- ЛОГИКА СМЕРТИ ИГРОКА ---
            if (currentCell.IsUnderEnemyAttack)
            {
                // Берем первого попавшегося врага, который простреливает эту клетку
                Vector2Int attackerPos = currentCell.AttackedBy[0];
            
                // Вызываем анимацию прыжка врага на игрока
                ExecuteEnemyRetaliation(attackerPos, toPos, movingPiece);
            }
            else
            {
                _isAnimating = false; // Всё безопасно, возвращаем инпут
            }
        });
    }
    private void ExecuteEnemyRetaliation(Vector2Int enemyPos, Vector2Int playerPos, PieceView playerPiece)
    {
        Debug.Log("Враг наносит ответный удар!");

        // 1. Двигаем врага в логике (он переписывает собой клетку игрока)
        _engine.MovePiece(enemyPos, playerPos);
    
        // Пересчитываем угрозы, так как враг сменил позицию
        _engine.UpdateThreatMap(); 
        RefreshBoardThreats();

        // 2. Обновляем словари визуала для врага
        PieceView retaliatingEnemy = _pieceViews[enemyPos];
        _pieceViews.Remove(enemyPos);
        _pieceViews[playerPos] = retaliatingEnemy; // Заменяем игрока в словаре
        retaliatingEnemy.LogicPosition = playerPos;

        // 3. Анимируем прыжок ВРАГА
        Vector3 targetWorldPos = _cellViews[playerPos].transform.position;
        targetWorldPos.y += 0.5f;

        retaliatingEnemy.MoveToWorldPosition(targetWorldPos, () =>
        {
            // Когда враг долетел — уничтожаем 3D-модельку игрока
            Destroy(playerPiece.gameObject);
        
            Debug.Log("Игрок убит! GAME OVER");
            // Здесь пока что просто оставляем _isAnimating = true; 
            // чтобы заблокировать клики после смерти, пока ты не сделаешь рестарт уровня.
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