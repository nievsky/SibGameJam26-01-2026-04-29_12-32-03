using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameController : MonoBehaviour
{
    [Header("Prefabs & Setup")]
    [SerializeField] private CellView _cellPrefab;
    [SerializeField] private float _cellSize = 1.1f;
    [SerializeField] private LayerMask _cellLayer;

    [Header("Кампания")]
    [SerializeField] private LevelData _currentLevel;
    public List<LevelData> CampaignLevels;
    private int _currentLevelIndex = 0;

    [Header("Префабы Фигур (Игрок)")]
    [SerializeField] private PieceView _playerKnight;
    [SerializeField] private PieceView _playerQueen;
    [SerializeField] private PieceView _playerBishop;
    [SerializeField] private PieceView _playerKing;
    [SerializeField] private PieceView _playerRook;
    [SerializeField] private PieceView _playerPawn;

    [Header("Префабы Фигур (Враг)")]
    [SerializeField] private PieceView _enemyRook;
    [SerializeField] private PieceView _enemyPawn;
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
    public int PointsPerCapture = 10;

    [Header("UI Звезды")]
    [SerializeField] private Image _starHintIcon;
    [SerializeField] private Image _starScore1Icon;
    [SerializeField] private Image _starScore2Icon;
    [SerializeField] private Color _starActiveColor = Color.yellow;
    [SerializeField] private Color _starInactiveColor = Color.gray;

    private int _currentScore = 0;
    private bool _isHintActive = false;
    private bool _hintsUsedThisLevel = false;

    private PieceType _editorSelectedPiece = PieceType.None;

    private ChessEngine _engine;
    private Dictionary<Vector2Int, CellView> _cellViews = new Dictionary<Vector2Int, CellView>();
    private Dictionary<Vector2Int, PieceView> _pieceViews = new Dictionary<Vector2Int, PieceView>();

    private PieceView _selectedPiece = null;
    private List<Vector2Int> _currentValidMoves = new List<Vector2Int>();
    private bool _isAnimating = false;

    private CellView _currentHoveredCell = null;

    private void Start()
    {
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
        _currentLevel = levelData;
        _engine = new ChessEngine(levelData.Width, levelData.Height);

        _currentScore = 0;
        _isHintActive = false;
        _hintsUsedThisLevel = false;

        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                CellSetup setup = levelData.Rows[y].Columns[x];

                _engine.GetCell(x, y).IsActive = setup.IsActive;

                Vector3 cellPos = new Vector3(x * _cellSize, 0, y * _cellSize);
                CellView cellView = Instantiate(_cellPrefab, cellPos, Quaternion.identity, transform);
                cellView.name = $"Cell {x}_{y}";

                Color baseColor = (x + y) % 2 == 0 ? Color.white : Color.gray;
                cellView.Init(new Vector2Int(x, y), baseColor, setup.IsActive);

                _cellViews.Add(new Vector2Int(x, y), cellView);

                if (setup.Piece != PieceType.None && setup.Alignment != Alignment.None)
                {
                    SpawnPieceFromSetup(new Vector2Int(x, y), setup);
                }
            }
        }

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        _inventoryAtLevelStart = new List<PieceType>(PlayerInventory);

        UpdateStarsUI();
    }

    private void SpawnPieceFromSetup(Vector2Int logicPos, CellSetup setup)
    {
        BoardCell cell = _engine.GetCell(logicPos);
        cell.CurrentPiece = setup.Piece;
        cell.PieceAlignment = setup.Alignment;

        if (setup.Alignment == Alignment.Player && !PlayerInventory.Contains(setup.Piece))
        {
            PlayerInventory.Add(setup.Piece);
            Debug.Log($"Стартовая фигура добавлена в инвентарь: {setup.Piece}");
        }

        UpdateCameraFocus(logicPos);

        PieceView prefabToSpawn = GetPrefab(setup.Piece, setup.Alignment);
        if (prefabToSpawn == null) return;

        Vector3 worldPos = _cellViews[logicPos].transform.position;
        worldPos.y += 0.5f;

        PieceView pieceView = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
        pieceView.LogicPosition = logicPos;
        pieceView.Type = setup.Piece;
        pieceView.Alignment = setup.Alignment;

        _pieceViews.Add(logicPos, pieceView);
    }

    private PieceView GetPrefab(PieceType type, Alignment alignment)
    {
        if (alignment == Alignment.Player)
        {
            switch (type)
            {
                case PieceType.Knight: return _playerKnight;
                case PieceType.Rook: return _playerRook;
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
                case PieceType.Knight: return _enemyKnight;
                case PieceType.Bishop: return _enemyBishop;
                case PieceType.Queen: return _enemyQueen;
                case PieceType.King: return _enemyKing;
            }
        }

        Debug.LogWarning($"Префаб для {type} ({alignment}) не назначен в GetPrefab!");
        return null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            IsEditMode = !IsEditMode;
            Debug.Log(IsEditMode ? "РЕЖИМ РЕДАКТОРА ВКЛЮЧЕН" : "РЕЖИМ РЕДАКТОРА ВЫКЛЮЧЕН");
            DeselectPiece();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleHints();
        }

        if (_isAnimating) return;

        if (IsEditMode)
        {
            HandleEditorInput();
        }
        else
        {
            HandleHover();
            HandleMouseInput();
        }
    }

    private void HandleEditorInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector2Int? targetPos = null;

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
        else if (Input.GetMouseButtonDown(1))
        {
            _editorSelectedPiece = PieceType.None;
            Debug.Log("Редактор: Кисть сброшена (режим строительства стен)");
        }
    }

    private void ProcessEditorClick(Vector2Int pos)
    {
        CellSetup setup = _currentLevel.Rows[pos.y].Columns[pos.x];
        BoardCell logicCell = _engine.GetCell(pos);

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
        else if (setup.IsActive && _editorSelectedPiece != PieceType.None)
        {
            setup.Piece = _editorSelectedPiece;
            setup.Alignment = Alignment.Enemy;

            SpawnPieceFromSetup(pos, setup);
        }
        else if (setup.IsActive && _editorSelectedPiece == PieceType.None)
        {
            setup.IsActive = false;
            logicCell.IsActive = false;
            _cellViews[pos].SetActiveState(false, Color.black);
        }
        else if (!setup.IsActive)
        {
            setup.IsActive = true;
            logicCell.IsActive = true;
            Color baseColor = (pos.x + pos.y) % 2 == 0 ? Color.white : Color.gray;
            _cellViews[pos].SetActiveState(true, baseColor);
        }

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

#if UNITY_EDITOR
        EditorUtility.SetDirty(_currentLevel);
        AssetDatabase.SaveAssets();
#endif
    }

    private void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            CellView targetCell = null;

            if (hit.collider.TryGetComponent(out CellView hitCell))
            {
                targetCell = hitCell;
            }
            else if (hit.collider.TryGetComponent(out PieceView hitPiece))
            {
                targetCell = _cellViews[hitPiece.LogicPosition];
            }

            if (targetCell != null && targetCell != _currentHoveredCell)
            {
                ClearHover();
                _currentHoveredCell = targetCell;

                if (!_currentValidMoves.Contains(_currentHoveredCell.LogicPosition))
                {
                    _currentHoveredCell.HighlightAsHover();
                }
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        if (_currentHoveredCell != null)
        {
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

                if (hit.collider.TryGetComponent(out CellView clickedCell))
                {
                    targetLogicPos = clickedCell.LogicPosition;
                }
                else if (hit.collider.TryGetComponent(out PieceView clickedPiece))
                {
                    targetLogicPos = clickedPiece.LogicPosition;

                    if (clickedPiece.Alignment == Alignment.Player)
                    {
                        SelectPiece(targetLogicPos.Value);
                        return;
                    }
                }

                if (targetLogicPos.HasValue)
                {
                    Vector2Int pos = targetLogicPos.Value;
                    BoardCell logicCell = _engine.GetCell(pos);

                    if (_selectedPiece != null && _currentValidMoves.Contains(pos))
                    {
                        ExecuteMove(_selectedPiece.LogicPosition, pos);
                    }
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
                    DeselectPiece();
                }
            }
            else
            {
                DeselectPiece();
            }
        }
    }

    private void SelectPiece(Vector2Int piecePos)
    {
        DeselectPiece();

        _selectedPiece = _pieceViews[piecePos];
        _currentValidMoves = _engine.GetValidMoves(piecePos);

        foreach (Vector2Int movePos in _currentValidMoves)
        {
            if (_engine.GetCell(movePos).HasEnemy)
                _cellViews[movePos].HighlightAsAttack();
            else
                _cellViews[movePos].HighlightAsMove();
        }

        AudioManager.PlayPickUpSound(_selectedPiece.transform.position);
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

        bool isKingCaptured = false; // Флаг победы

        // --- ЛОГИКА РУБКИ ВРАГА ИГРОКОМ ---
        if (_pieceViews.TryGetValue(toPos, out PieceView enemyPiece))
        {
            if (enemyPiece.Alignment == Alignment.Enemy)
            {
                // Если мы рубим Короля - запоминаем это!
                if (enemyPiece.Type == PieceType.King)
                {
                    isKingCaptured = true;
                }

                _currentScore += PointsPerCapture;
                Debug.Log($"Враг срублен! Очки: {_currentScore}");

                UpdateStarsUI();

                if (!PlayerInventory.Contains(enemyPiece.Type))
                {
                    PlayerInventory.Add(enemyPiece.Type);
                }

                Destroy(enemyPiece.gameObject);
                _pieceViews.Remove(toPos);
            }
        }

        // 1. Двигаем в логике движка
        _engine.MovePiece(fromPos, toPos);
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        // 2. Обновляем словари визуала
        PieceView movingPiece = _pieceViews[fromPos];
        _pieceViews.Remove(fromPos);
        _pieceViews[toPos] = movingPiece;
        movingPiece.LogicPosition = toPos;

        // 3. Обновляем камеру
        UpdateCameraFocus(toPos);

        // 4. Анимируем визуал
        Vector3 targetWorldPos = _cellViews[toPos].transform.position;
        targetWorldPos.y += 0.5f;

        movingPiece.MoveToWorldPosition(targetWorldPos, () =>
        {
            // --- ПРОВЕРКА ПОБЕДЫ ---
            if (isKingCaptured)
            {
                _isAnimating = false;
                Debug.Log("КОРОЛЬ ПОВЕРЖЕН! УРОВЕНЬ ПРОЙДЕН!");
                EvaluateLevelStars();
                LoadNextLevel();
                return; // Прерываем логику (уровень завершен, ответный удар врага не срабатывает)
            }

            if (isCapture)
            {
                AudioManager.PlayCaptureSound(targetWorldPos);
            }
            else
            {
                AudioManager.PlayPlaceSound(targetWorldPos);
            }

            BoardCell currentCell = _engine.GetCell(toPos);

            // --- ЛОГИКА СМЕРТИ ИГРОКА (если это не победный ход) ---
            if (currentCell.IsUnderEnemyAttack)
            {
                Vector2Int attackerPos = currentCell.AttackedBy[0];
                ExecuteEnemyRetaliation(attackerPos, toPos, movingPiece);
            }
            else
            {
                _isAnimating = false; // Возвращаем инпут, если всё безопасно
            }
        });
    }

    private void ExecuteEnemyRetaliation(Vector2Int enemyPos, Vector2Int playerPos, PieceView playerPiece)
    {
        Debug.Log("Враг наносит ответный удар!");

        Vector3 attackPos = _cellViews[playerPos].transform.position;
        attackPos.y += 0.5f;
        AudioManager.PlayEnemyAttackSound(attackPos);

        _engine.MovePiece(enemyPos, playerPos);

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        PieceView retaliatingEnemy = _pieceViews[enemyPos];
        _pieceViews.Remove(enemyPos);
        _pieceViews[playerPos] = retaliatingEnemy;
        retaliatingEnemy.LogicPosition = playerPos;

        Vector3 targetWorldPos = _cellViews[playerPos].transform.position;
        targetWorldPos.y += 0.5f;

        retaliatingEnemy.MoveToWorldPosition(targetWorldPos, () =>
        {
            Vector3 deathPos = _cellViews[playerPos].transform.position;
            deathPos.y += 0.5f;
            AudioManager.PlayKillSound(deathPos);
            AudioManager.PlayPlaceSound(targetWorldPos);
            Destroy(playerPiece.gameObject);
            RestartLevel();
        });
    }

    private void RefreshBoardThreats()
    {
        foreach (var kvp in _cellViews)
        {
            Vector2Int pos = kvp.Key;
            CellView view = kvp.Value;

            bool shouldShowThreat = IsEditMode || _isHintActive;

            view.IsThreatened = shouldShowThreat && _engine.GetCell(pos).IsUnderEnemyAttack;
            view.ResetHighlight();
        }
    }

    public void OnCardClicked(int typeIndex)
    {
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

        PieceView newPrefab = GetPrefab(newType, Alignment.Player);
        if (newPrefab == null)
        {
            Debug.LogError($"МОРФ ОТМЕНЕН: Префаб для {newType} не найден!");
            return;
        }

        _engine.GetCell(playerPos).CurrentPiece = newType;

        PieceView oldView = _pieceViews[playerPos];
        Destroy(oldView.gameObject);
        _pieceViews.Remove(playerPos);

        Vector3 transformPos = _cellViews[playerPos].transform.position;
        transformPos.y += 0.5f;
        AudioManager.PlayTransformationSound(transformPos);

        Vector3 worldPos = _cellViews[playerPos].transform.position;
        worldPos.y += 0.5f;

        PieceView newView = Instantiate(newPrefab, worldPos, Quaternion.identity);
        newView.LogicPosition = playerPos;
        newView.Type = newType;
        newView.Alignment = Alignment.Player;
        _pieceViews.Add(playerPos, newView);

        if (_selectedPiece != null && _selectedPiece.LogicPosition == playerPos)
        {
            SelectPiece(playerPos);
        }

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        Debug.Log($"МОРФ: Фигура игрока изменена на {newType}");
        
        UpdateCameraFocus(playerPos);
    }

    private void ClearBoard()
    {
        foreach (var cell in _cellViews.Values) Destroy(cell.gameObject);
        foreach (var piece in _pieceViews.Values) Destroy(piece.gameObject);

        _cellViews.Clear();
        _pieceViews.Clear();
        _selectedPiece = null;
        _currentValidMoves.Clear();
        _isAnimating = false;
    }

    private void RestartLevel()
    {
        Debug.Log("--- РЕСТАРТ УРОВНЯ ---");

        if (Camera.main != null)
        {
            AudioManager.PlayRestartSound(Camera.main.transform.position);
        }
        else
        {
            AudioManager.PlayRestartSound();
        }

        PlayerInventory = new List<PieceType>(_inventoryAtLevelStart);

        ClearBoard();
        LoadLevel(_currentLevel);

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
            ClearBoard();
        }
    }

    private bool IsEnemyKingInCheck()
    {
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

        List<Vector2Int> attackRange = _engine.GetValidMoves(playerPos, true);

        foreach (Vector2Int target in attackRange)
        {
            BoardCell cell = _engine.GetCell(target);
            if (cell.HasEnemy && cell.CurrentPiece == PieceType.King)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateCameraFocus(Vector2Int playerLogicPos)
    {
        if (_cameraController != null && _cellViews.ContainsKey(playerLogicPos))
        {
            Vector3 worldPos = _cellViews[playerLogicPos].transform.position;
            _cameraController.TargetFocusPosition = worldPos;
        }
    }

    public void ToggleHints()
    {
        if (IsEditMode) return;

        _isHintActive = !_isHintActive;

        if (_isHintActive)
        {
            _hintsUsedThisLevel = true;
        }

        RefreshBoardThreats();
        Debug.Log($"Подсказки: {(_isHintActive ? "ВКЛ" : "ВЫКЛ")}. Использованы за уровень: {_hintsUsedThisLevel}");

        UpdateStarsUI();
    }

    private void EvaluateLevelStars()
    {
        int stars = 0;

        if (_currentScore >= _currentLevel.Star1ScoreThreshold) stars++;
        if (_currentScore >= _currentLevel.Star2ScoreThreshold) stars++;
        if (!_hintsUsedThisLevel) stars++;

        Debug.Log("=============================");
        Debug.Log($"УРОВЕНЬ ПРОЙДЕН! Очки: {_currentScore}");
        Debug.Log($"Подсказки использованы: {_hintsUsedThisLevel}");
        Debug.Log($"ПОЛУЧЕНО ЗВЕЗД: {stars} / 3");
        Debug.Log("=============================");
    }

    private void UpdateStarsUI()
    {
        if (_starHintIcon != null)
        {
            _starHintIcon.color = !_hintsUsedThisLevel ? _starActiveColor : _starInactiveColor;
        }

        if (_starScore1Icon != null)
        {
            _starScore1Icon.color = _currentScore >= _currentLevel.Star1ScoreThreshold ? _starActiveColor : _starInactiveColor;
        }

        if (_starScore2Icon != null)
        {
            _starScore2Icon.color = _currentScore >= _currentLevel.Star2ScoreThreshold ? _starActiveColor : _starInactiveColor;
        }
    }
}