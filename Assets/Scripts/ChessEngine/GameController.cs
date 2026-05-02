using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor; // Нужно для сохранения ScriptableObject
#endif

public class GameController : MonoBehaviour
{
    [Header("Prefabs & Setup")]
    [SerializeField] private CellView _cellPrefab;
    [SerializeField] private float _cellSize = 1.1f;  // Расстояние между 3D клетками
    [SerializeField] private LayerMask _cellLayer; 
    
    [Header("Кампания")]
    [SerializeField] private LevelData _currentLevel;
    public List<LevelData> CampaignLevels; // Перетащи сюда все свои файлы уровней по порядку
    private int _currentLevelIndex = 0;

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
    
    [Header("Инвентарь и UI")]
    public List<PieceType> PlayerInventory = new List<PieceType>();
    private List<PieceType> _inventoryAtLevelStart = new List<PieceType>();
    
    [Header("Редактор Уровней")]
    public bool IsEditMode = false;
    
    [Header("Ссылки на системы")]
    [SerializeField] private CameraController _cameraController;
    
    [Header("Прогрессия и Звезды")]
    public int PointsPerCapture = 10; // Фиксированное количество очков за срубленную фигуру
    
    [Header("UI Звезды")]
    [SerializeField] private Image _starHintIcon;   // Звезда "Без подсказок" (по дефолту горит)
    [SerializeField] private Image _starScore1Icon; // Звезда за 1-й порог очков
    [SerializeField] private Image _starScore2Icon; // Звезда за 2-й порог очков
    [SerializeField] private Color _starActiveColor = Color.yellow;
    [SerializeField] private Color _starInactiveColor = Color.gray; // Или полупрозрачный черный
    
    private int _currentScore = 0;
    private bool _isHintActive = false;       // Текущее состояние подсказок
    private bool _hintsUsedThisLevel = false; // Использовал ли игрок подсказку хоть раз за уровень
    
    // Переменная для хранения выбранной "кисти" в редакторе
    private PieceType _editorSelectedPiece = PieceType.None;

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
        // Если в кампании есть уровни - грузим первый, иначе грузим то, что в _currentLevel
        if (CampaignLevels != null && CampaignLevels.Count > 0)
        {
            LoadLevel(CampaignLevels[0]);
        }
        else if (_currentLevel != null)
        {
            LoadLevel(_currentLevel);
        }
    }
    
    private void LoadLevel(LevelData levelData)
    {
        _currentLevel = levelData; // Обновляем ссылку, чтобы редактор знал, куда сохранять!
        _engine = new ChessEngine(levelData.Width, levelData.Height);
        
        // --- СБРОС ПРОГРЕССИИ ---
        _currentScore = 0;
        _isHintActive = false;
        _hintsUsedThisLevel = false;

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
        
        _inventoryAtLevelStart = new List<PieceType>(PlayerInventory);
        
        // --- ОБНОВЛЯЕМ UI ПРИ СТАРТЕ ---
        UpdateStarsUI();
    }
    
    private void SpawnPieceFromSetup(Vector2Int logicPos, CellSetup setup)
    {
        // 1. Записываем в логику
        BoardCell cell = _engine.GetCell(logicPos);
        cell.CurrentPiece = setup.Piece;
        cell.PieceAlignment = setup.Alignment;
        
        if (setup.Alignment == Alignment.Player && !PlayerInventory.Contains(setup.Piece))
        {
            PlayerInventory.Add(setup.Piece);
            Debug.Log($"Стартовая фигура добавлена в инвентарь: {setup.Piece}");
        }
        
        UpdateCameraFocus(logicPos);
        
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
                case PieceType.Rook: return _playerRook;
                // Добавь сюда остальных игроков, когда будут готовы префабы
                case PieceType.Bishop: return _playerBishop;
                case PieceType.Queen: return _playerQueen;
                case PieceType.King: return _playerKing;
                case PieceType.Pawn: return _playerPawn;
            }
        }
        else if (alignment == Alignment.Enemy)
        {
            switch (type)
            {
                case PieceType.Rook: return _enemyRook;
                case PieceType.Pawn: return _enemyPawn;
                case PieceType.Knight: return _enemyKnight; // Добавили
                case PieceType.Bishop: return _enemyBishop; // Добавили
                case PieceType.Queen: return _enemyQueen;   // Добавили
                case PieceType.King: return _enemyKing;     // Добавили
            }
        }
        
        Debug.LogWarning($"Префаб для {type} ({alignment}) не назначен в GetPrefab!");
        return null;
    }

    private void Update()
    {
        // Включение/выключение режима редактора на клавишу TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            IsEditMode = !IsEditMode;
            Debug.Log(IsEditMode ? "РЕЖИМ РЕДАКТОРА ВКЛЮЧЕН" : "РЕЖИМ РЕДАКТОРА ВЫКЛЮЧЕН");
            DeselectPiece();
        }
        
        // В дальнейшем забиндить на кнопку в UI, а пока - на клавишу H для удобства тестирования
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleHints();
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
        if (Input.GetMouseButtonDown(0)) // Левый клик - основное действие
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector2Int? targetPos = null;

                // Проверяем, попали мы в клетку или сразу в фигуру на ней
                if (hit.collider.TryGetComponent(out CellView clickedCell))
                    targetPos = clickedCell.LogicPosition;
                else if (hit.collider.TryGetComponent(out PieceView clickedPiece))
                    targetPos = clickedPiece.LogicPosition;

                if (targetPos.HasValue)
                {
                    ProcessEditorClick(targetPos.Value);
                }
            }
        }
        else if (Input.GetMouseButtonDown(1)) // Правый клик - сбросить кисть фигуры
        {
            _editorSelectedPiece = PieceType.None;
            Debug.Log("Редактор: Кисть сброшена (режим строительства стен)");
        }
    }
    
    private void ProcessEditorClick(Vector2Int pos)
    {
        CellSetup setup = _currentLevel.Rows[pos.y].Columns[pos.x];
        BoardCell logicCell = _engine.GetCell(pos);

        // СЦЕНАРИЙ 1: На клетке стоит фигура -> Удаляем её
        if (!logicCell.IsEmpty)
        {
            setup.Piece = PieceType.None;
            setup.Alignment = Alignment.None;
            logicCell.ClearPiece();
            
            if (_pieceViews.TryGetValue(pos, out PieceView pieceToRemove))
            {
                Destroy(pieceToRemove.gameObject);
                _pieceViews.Remove(pos);
            }
        }
        // СЦЕНАРИЙ 2: Клетка пуста, активна и выбрана "кисть" фигуры -> Ставим врага
        else if (setup.IsActive && _editorSelectedPiece != PieceType.None)
        {
            setup.Piece = _editorSelectedPiece;
            setup.Alignment = Alignment.Enemy; // По умолчанию дизайнер расставляет врагов
            
            SpawnPieceFromSetup(pos, setup);
        }
        // СЦЕНАРИЙ 3: Клетка пуста, активна, кисть НЕ выбрана -> Делаем стену (неактивной)
        else if (setup.IsActive && _editorSelectedPiece == PieceType.None)
        {
            setup.IsActive = false;
            logicCell.IsActive = false;
            _cellViews[pos].SetActiveState(false, Color.black); // Или передай сюда свой _inactiveColor
        }
        // СЦЕНАРИЙ 4: Клетка неактивна (стена) -> Делаем активной (пол)
        else if (!setup.IsActive)
        {
            setup.IsActive = true;
            logicCell.IsActive = true;
            Color baseColor = (pos.x + pos.y) % 2 == 0 ? Color.white : Color.gray;
            _cellViews[pos].SetActiveState(true, baseColor);
        }

        // Обновляем красную подсветку угроз после любых изменений
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        // СОХРАНЯЕМ ФАЙЛ SCRIPTABLE OBJECT
        #if UNITY_EDITOR
        EditorUtility.SetDirty(_currentLevel);
        AssetDatabase.SaveAssets();
        #endif
    }

    // НОВЫЙ МЕТОД: Переключение клетки и сохранение в фай
    
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
        DeselectPiece(); 

        _selectedPiece = _pieceViews[piecePos];
        // Вызываем без флага, так как это расчет обычных ходов игрока
        _currentValidMoves = _engine.GetValidMoves(piecePos); 

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
                // Начисляем очки
                _currentScore += PointsPerCapture;
                Debug.Log($"Враг срублен! Очки: {_currentScore}");
                
                // --- ОБНОВЛЯЕМ UI (могут загореться звезды за очки) ---
                UpdateStarsUI();

                if (!PlayerInventory.Contains(enemyPiece.Type))
                {
                    PlayerInventory.Add(enemyPiece.Type);
                }

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
        
        UpdateCameraFocus(toPos);

        movingPiece.MoveToWorldPosition(targetWorldPos, () => 
        {
            BoardCell currentCell = _engine.GetCell(toPos);

            if (currentCell.IsUnderEnemyAttack)
            {
                // Попали в ловушку - враг прыгает на нас
                Vector2Int attackerPos = currentCell.AttackedBy[0];
                ExecuteEnemyRetaliation(attackerPos, toPos, movingPiece);
            }
            else
            {
                _isAnimating = false; // Возвращаем инпут

                // --- НОВАЯ ПРОВЕРКА НА ШАХ ---
                if (IsEnemyKingInCheck())
                {
                    Debug.Log("ШАХ ВРАЖЕСКОМУ КОРОЛЮ!");
                    EvaluateLevelStars();
                    LoadNextLevel();
                }
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
            Destroy(playerPiece.gameObject); // Убиваем модельку игрока
            RestartLevel();                  // <--- ЗАПУСКАЕМ РЕСТАРТ!
        });
    }
    
    private void RefreshBoardThreats()
    {
        foreach (var kvp in _cellViews)
        {
            Vector2Int pos = kvp.Key;
            CellView view = kvp.Value;
            
            // Клетка светится красным ТОЛЬКО если подсказки включены (или включен редактор)
            bool shouldShowThreat = IsEditMode || _isHintActive;
            
            view.IsThreatened = shouldShowThreat && _engine.GetCell(pos).IsUnderEnemyAttack;
            view.ResetHighlight(); 
        }
    }
    
    public void OnCardClicked(int typeIndex)
    {
        // Конвертируем цифру обратно в тип фигуры
        PieceType type = (PieceType)typeIndex;

        if (IsEditMode)
        {
            _editorSelectedPiece = type;
            Debug.Log($"Редактор: Выбрана кисть - {type}");
        }
        else
        {
            if (PlayerInventory.Contains(type))
            {
                SwapPlayerPiece(type);
            }
        }
    }
    
    private void SwapPlayerPiece(PieceType newType)
    {
        // 1. Ищем, где сейчас стоит игрок
        Vector2Int playerPos = Vector2Int.zero;
        bool foundPlayer = false;
        
        foreach (var kvp in _pieceViews)
        {
            if (kvp.Value.Alignment == Alignment.Player)
            {
                playerPos = kvp.Key;
                foundPlayer = true;
                break;
            }
        }

        if (!foundPlayer) return;

        // 2. ПРОВЕРЯЕМ ПРЕФАБ ДО ТОГО КАК УДАЛЯТЬ СТАРУЮ ФИГУРУ!
        PieceView newPrefab = GetPrefab(newType, Alignment.Player);
        if (newPrefab == null) 
        {
            Debug.LogError($"МОРФ ОТМЕНЕН: Префаб для {newType} не найден! Проверь метод GetPrefab и инспектор.");
            return; 
        }

        // 3. Обновляем логический движок
        _engine.GetCell(playerPos).CurrentPiece = newType;

        // 4. Удаляем старую 3D-модель
        PieceView oldView = _pieceViews[playerPos];
        Destroy(oldView.gameObject);
        _pieceViews.Remove(playerPos);

        // 5. Спавним новую 3D-модель
        Vector3 worldPos = _cellViews[playerPos].transform.position;
        worldPos.y += 0.5f;

        PieceView newView = Instantiate(newPrefab, worldPos, Quaternion.identity);
        newView.LogicPosition = playerPos;
        newView.Type = newType;
        newView.Alignment = Alignment.Player;
        _pieceViews.Add(playerPos, newView);

        // 6. Перерисовываем подсветку ходов, если игрок был выделен
        if (_selectedPiece != null && _selectedPiece.LogicPosition == playerPos)
        {
            SelectPiece(playerPos);
        }

        // Обновляем угрозы, так как у новой фигуры другие линии перекрытия
        _engine.UpdateThreatMap();
        RefreshBoardThreats();
        
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        Debug.Log($"МОРФ: Фигура игрока изменена на {newType}");

        // --- НОВАЯ ПРОВЕРКА НА ШАХ ПОСЛЕ ПРЕВРАЩЕНИЯ ---
        if (IsEnemyKingInCheck())
        {
            Debug.Log("ШАХ ПОСЛЕ ПРЕВРАЩЕНИЯ!");
            EvaluateLevelStars();
            LoadNextLevel();
        }
        
        UpdateCameraFocus(playerPos);
    }
    
    private void ClearBoard()
    {
        // Уничтожаем все 3D объекты со сцены
        foreach (var cell in _cellViews.Values) Destroy(cell.gameObject);
        foreach (var piece in _pieceViews.Values) Destroy(piece.gameObject);
        
        // Очищаем словари
        _cellViews.Clear();
        _pieceViews.Clear();
        _selectedPiece = null;
        _currentValidMoves.Clear();
        _isAnimating = false;
    }

    private void RestartLevel()
    {
        Debug.Log("--- РЕСТАРТ УРОВНЯ ---");
        // Откатываем инвентарь к тому состоянию, каким он был в начале уровня
        PlayerInventory = new List<PieceType>(_inventoryAtLevelStart); 
        
        ClearBoard();
        LoadLevel(_currentLevel); // Перезагружаем текущий файл
        
        // --- МГНОВЕННО ПЕРЕНОСИМ КАМЕРУ К ИГРОКУ ---
        if (_cameraController != null)
        {
            _cameraController.SnapToTarget();
        }
    }

    private void LoadNextLevel()
    {
        _currentLevelIndex++;
        
        if (_currentLevelIndex < CampaignLevels.Count)
        {
            Debug.Log($"--- ПЕРЕХОД НА УРОВЕНЬ {_currentLevelIndex + 1} ---");
            ClearBoard();
            LoadLevel(CampaignLevels[_currentLevelIndex]);
        }
        else
        {
            Debug.Log("ПОБЕДА! Кампания пройдена!");
            ClearBoard(); // Очищаем доску в конце игры
        }
    }
    
    private bool IsEnemyKingInCheck()
    {
        // 1. Ищем позицию игрока
        Vector2Int playerPos = Vector2Int.zero;
        bool found = false;
        foreach (var kvp in _pieceViews)
        {
            if (kvp.Value.Alignment == Alignment.Player)
            {
                playerPos = kvp.Key;
                found = true;
                break;
            }
        }

        if (!found) return false;

        // 2. Получаем все клетки, которые простреливает игрок
        // Передаем isForThreatMap = true, так как нас интересует ЗОНА АТАКИ (важно для Пешки!)
        List<Vector2Int> attackRange = _engine.GetValidMoves(playerPos, true);
        
        // 3. Проверяем, стоит ли на одной из этих клеток Король
        foreach (Vector2Int target in attackRange)
        {
            BoardCell cell = _engine.GetCell(target);
            if (cell.HasEnemy && cell.CurrentPiece == PieceType.King)
            {
                return true; // ШАХ!
            }
        }
        return false;
    }
    
    private void UpdateCameraFocus(Vector2Int playerLogicPos)
    {
        if (_cameraController != null && _cellViews.ContainsKey(playerLogicPos))
        {
            // Берем координаты 3D-клетки, на которой стоит игрок
            Vector3 worldPos = _cellViews[playerLogicPos].transform.position;
            _cameraController.TargetFocusPosition = worldPos;
        }
    }
    
    public void ToggleHints()
    {
        if (IsEditMode) return; // В редакторе не переключаем

        _isHintActive = !_isHintActive;
        
        // Если включили подсказку - отмечаем, что игрок потерял третью звезду
        if (_isHintActive) 
        {
            _hintsUsedThisLevel = true; 
        }

        RefreshBoardThreats(); // Перерисовываем доску
        Debug.Log($"Подсказки: {(_isHintActive ? "ВКЛ" : "ВЫКЛ")}. Использованы за уровень: {_hintsUsedThisLevel}");
        
        // --- ОБНОВЛЯЕМ UI (звезда за подсказку погаснет) ---
        UpdateStarsUI();
    }
    
    private void EvaluateLevelStars()
    {
        int stars = 0;

        // 1-я звезда (Очки)
        if (_currentScore >= _currentLevel.Star1ScoreThreshold) stars++;
        
        // 2-я звезда (Очки)
        if (_currentScore >= _currentLevel.Star2ScoreThreshold) stars++;
        
        // 3-я звезда (Без подсказок)
        if (!_hintsUsedThisLevel) stars++;

        Debug.Log("=============================");
        Debug.Log($"УРОВЕНЬ ПРОЙДЕН! Очки: {_currentScore}");
        Debug.Log($"Подсказки использованы: {_hintsUsedThisLevel}");
        Debug.Log($"ПОЛУЧЕНО ЗВЕЗД: {stars} / 3");
        Debug.Log("=============================");
        
        // Здесь в будущем ты сможешь вызвать UI панель победы 
        // и передать ей количество звезд перед загрузкой следующего уровня
    }
    
    
    private void UpdateStarsUI()
    {
        // 1. Звезда за подсказки (горит, если подсказки НЕ использовались)
        if (_starHintIcon != null)
        {
            _starHintIcon.color = !_hintsUsedThisLevel ? _starActiveColor : _starInactiveColor;
        }

        // 2. Звезда за первый порог очков
        if (_starScore1Icon != null)
        {
            _starScore1Icon.color = _currentScore >= _currentLevel.Star1ScoreThreshold ? _starActiveColor : _starInactiveColor;
        }

        // 3. Звезда за второй порог очков
        if (_starScore2Icon != null)
        {
            _starScore2Icon.color = _currentScore >= _currentLevel.Star2ScoreThreshold ? _starActiveColor : _starInactiveColor;
        }
    }
}