using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
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

    [Header("Настройки мыши")]
    [SerializeField] private LayerMask _boardLayerMask;

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

    [Header("UI Звезды (DOTween)")]
    [SerializeField] private GameObject[] _filledStarObjects = new GameObject[3];
    [SerializeField] private float _starAnimDuration = 0.4f;
    [SerializeField] private float _starAnimDelay = 0.2f;

    [Header("UI Инвентаря")]
    [SerializeField] private List<InventorySlotUI> _inventorySlots;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject _morphParticlePrefab;

    [Header("UI Экран Победы")]
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private TextMeshProUGUI _levelProgressText;
    [SerializeField] private GameObject[] _victoryStarObjects = new GameObject[3];

    [Header("Кнопки Победного Экрана")]
    [SerializeField] private GameObject _nextLevelButton;
    [SerializeField] private GameObject _restartButton;
    [SerializeField] private GameObject _cutsceneButton;
    [SerializeField] private GameObject _mainMenuButton;

    [Header("Настройки анимации")]
    [SerializeField] private float _pieceHoverHeight = 0.3f;
    [SerializeField] private float _animationDuration = 0.2f;

    [Header("Текстуры доски")]
    [SerializeField] private Texture2D _lightCellTex;
    [SerializeField] private Texture2D _darkCellTex;

    public static int TargetStartLevelIndex = -1;

    private int _currentlyDisplayedStars = 0;

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
        if (TargetStartLevelIndex >= 0 && CampaignLevels != null && TargetStartLevelIndex < CampaignLevels.Count)
        {
            _currentLevelIndex = TargetStartLevelIndex;
            LoadLevel(CampaignLevels[_currentLevelIndex]);
        }
        else if (CampaignLevels != null && CampaignLevels.Count > 0)
        {
            _currentLevelIndex = 0;
            LoadLevel(CampaignLevels[0]);
        }
        else if (_currentLevel != null)
        {
            LoadLevel(_currentLevel);
        }
    }

    private void OnDestroy()
    {
        StopCoroutine("StartAudioWithDelay");
        AudioManager.StopAllPersistentAudio();
        PauseAudioManager.StopSnapshot();
    }

    private IEnumerator StartAudioWithDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        AudioManager.PlayLevelMusic();
        AudioManager.PlayAmbience();
    }

    private void LoadLevel(LevelData levelData)
    {
        if (_victoryPanel != null) _victoryPanel.SetActive(false);
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

                Vector3 cellPos = GetCenteredWorldPosition(x, y, levelData.Width, levelData.Height);

                CellView cellView = Instantiate(_cellPrefab, cellPos, Quaternion.identity, transform);
                cellView.name = $"Cell {x}_{y}";

                Texture2D correctTex = (x + y) % 2 == 0 ? _lightCellTex : _darkCellTex;
                cellView.Init(new Vector2Int(x, y), correctTex, setup.IsActive);
                _cellViews.Add(new Vector2Int(x, y), cellView);

                if (setup.IsActive && setup.Piece != PieceType.None && setup.Alignment != Alignment.None)
                {
                    SpawnPieceFromSetup(new Vector2Int(x, y), setup);
                }
            }
        }

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        _inventoryAtLevelStart = new List<PieceType>(PlayerInventory);

        if (_cameraController != null)
        {
            _cameraController.SetupBoard(levelData.Width, levelData.Height, _cellSize, transform.position);
        }

        _currentlyDisplayedStars = 0;
        foreach (var star in _filledStarObjects)
        {
            star.SetActive(false);
            star.transform.localScale = Vector3.one;
        }

        UpdateStarsUI();
        FocusCameraOnPlayerInstant();
        UpdateInventoryUI();

        // Запускаем музыку и эмбиенс после полной загрузки уровня
        StopCoroutine("StartAudioWithDelay");
        StartCoroutine(StartAudioWithDelay(0.05f));
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

        PieceView prefabToSpawn = GetPrefab(setup.Piece, setup.Alignment);
        if (prefabToSpawn == null) return;

        Vector3 worldPos = _cellViews[logicPos].transform.position;
        worldPos.y += 0.1f;

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

            _cellViews[pos].SetActiveState(false, null);
        }
        else if (!setup.IsActive)
        {
            setup.IsActive = true;
            logicCell.IsActive = true;

            Texture2D correctTex = (pos.x + pos.y) % 2 == 0 ? _lightCellTex : _darkCellTex;

            _cellViews[pos].SetActiveState(true, correctTex);
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

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _boardLayerMask))
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
        if (_selectedPiece != null && _selectedPiece.LogicPosition == piecePos)
        {
            DeselectPiece();
            return;
        }

        DeselectPiece();

        _selectedPiece = _pieceViews[piecePos];
        _currentValidMoves = _engine.GetValidMoves(piecePos);

        float targetY = _cellViews[piecePos].transform.position.y + 0.1f + _pieceHoverHeight;
        _selectedPiece.transform.DOMoveY(targetY, _animationDuration).SetEase(Ease.OutQuad);

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
        if (_selectedPiece == null) return;

        if (_selectedPiece.gameObject != null)
        {
            float baseY = _cellViews[_selectedPiece.LogicPosition].transform.position.y + 0.1f;
            _selectedPiece.transform.DOMoveY(baseY, _animationDuration).SetEase(Ease.InQuad);
        }

        foreach (Vector2Int movePos in _currentValidMoves)
        {
            if (_cellViews.ContainsKey(movePos))
                _cellViews[movePos].ResetHighlight();
        }

        _currentValidMoves.Clear();
        _selectedPiece = null;
    }

    private void ExecuteMove(Vector2Int fromPos, Vector2Int toPos)
    {
        DeselectPiece();
        _isAnimating = true;

        bool isKingCaptured = false;
        bool isCapture = false;

        if (_pieceViews.TryGetValue(toPos, out PieceView enemyPiece))
        {
            if (enemyPiece.Alignment == Alignment.Enemy)
            {
                isCapture = true;

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
                    UpdateInventoryUI();
                }

                Destroy(enemyPiece.gameObject);
                _pieceViews.Remove(toPos);
            }
        }

        _engine.MovePiece(fromPos, toPos);
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        PieceView movingPiece = _pieceViews[fromPos];
        _pieceViews.Remove(fromPos);
        _pieceViews[toPos] = movingPiece;
        movingPiece.LogicPosition = toPos;

        UpdateCameraFocus(toPos);

        Vector3 targetWorldPos = _cellViews[toPos].transform.position;
        targetWorldPos.y += 0.1f;

        movingPiece.MoveToWorldPosition(targetWorldPos, () =>
        {
            if (isKingCaptured)
            {
                AudioManager.PlayKingAngrySound();
                AudioManager.PlayWinSound();
                Debug.Log("КОРОЛЬ ПОВЕРЖЕН!");
                ShowVictoryScreen();
                return;
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

            if (currentCell.IsUnderEnemyAttack)
            {
                Vector2Int attackerPos = currentCell.AttackedBy[0];
                ExecuteEnemyRetaliation(attackerPos, toPos, movingPiece);
            }
            else
            {
                _isAnimating = false;
            }
        });
    }

    private void ExecuteEnemyRetaliation(Vector2Int enemyPos, Vector2Int playerPos, PieceView playerPiece)
    {
        Debug.Log("Враг наносит ответный удар!");

        Vector3 attackPos = _cellViews[playerPos].transform.position;
        attackPos.y += 0.1f;
        AudioManager.PlayEnemyAttackSound(attackPos);

        _engine.MovePiece(enemyPos, playerPos);

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        PieceView retaliatingEnemy = _pieceViews[enemyPos];
        _pieceViews.Remove(enemyPos);
        _pieceViews[playerPos] = retaliatingEnemy;
        retaliatingEnemy.LogicPosition = playerPos;

        Vector3 targetWorldPos = _cellViews[playerPos].transform.position;
        targetWorldPos.y += 0.1f;

        retaliatingEnemy.MoveToWorldPosition(targetWorldPos, () =>
        {
            Vector3 deathPos = _cellViews[playerPos].transform.position;
            deathPos.y += 0.1f;

            AudioManager.PlayKingLaughSound();
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

        Vector3 worldPos = _cellViews[playerPos].transform.position;
        worldPos.y += 0.1f;

        if (_morphParticlePrefab != null)
        {
            Vector3 particlePos = worldPos;
            particlePos.y += 0.1f;

            GameObject effect = Instantiate(_morphParticlePrefab, particlePos, Quaternion.identity);
            Destroy(effect, 2f);
        }

        Vector3 transformPos = _cellViews[playerPos].transform.position;
        transformPos.y += 0.1f;
        AudioManager.PlayChangeSound(transformPos);

        PieceView oldView = _pieceViews[playerPos];
        Destroy(oldView.gameObject);
        _pieceViews.Remove(playerPos);

        PieceView newView = Instantiate(newPrefab, worldPos, Quaternion.identity);
        newView.LogicPosition = playerPos;
        newView.Type = newType;
        newView.Alignment = Alignment.Player;
        _pieceViews.Add(playerPos, newView);

        if (_selectedPiece != null && _selectedPiece.LogicPosition == playerPos)
        {
            SelectPiece(playerPos);
        }

        UpdateCameraFocus(playerPos);
        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        Debug.Log($"МОРФ: Фигура игрока изменена на {newType}");
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

        UpdateInventoryUI();

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
            PlayerInventory.Clear();
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
        int finalStars = CalculateEarnedStars();

        Debug.Log("=============================");
        Debug.Log($"УРОВЕНЬ ПРОЙДЕН! Очки: {_currentScore}");
        Debug.Log($"ПОЛУЧЕНО ЗВЕЗД: {finalStars} / 3");
        Debug.Log("=============================");

        string levelKey = $"LevelProgress_{_currentLevel.name}";

        int previousStars = PlayerPrefs.GetInt(levelKey, 0);

        if (finalStars > previousStars)
        {
            PlayerPrefs.SetInt(levelKey, finalStars);
            PlayerPrefs.Save();
            Debug.Log($"Новый рекорд сохранен! Уровень: {_currentLevel.name}, Звезды: {finalStars}");
        }
    }

    private void UpdateStarsUI()
    {
        int targetStars = CalculateEarnedStars();

        if (targetStars > _currentlyDisplayedStars)
        {
            StartCoroutine(AnimateStarsRoutine(_currentlyDisplayedStars, targetStars));
            _currentlyDisplayedStars = targetStars;
        }
        else if (targetStars < _currentlyDisplayedStars)
        {
            for (int i = targetStars; i < _currentlyDisplayedStars; i++)
            {
                _filledStarObjects[i].SetActive(false);
            }
            _currentlyDisplayedStars = targetStars;
        }
    }

    private void FocusCameraOnPlayerInstant()
    {
        foreach (var kvp in _pieceViews)
        {
            if (kvp.Value.Alignment == Alignment.Player)
            {
                UpdateCameraFocus(kvp.Key);

                if (_cameraController != null)
                    _cameraController.SnapToTarget();

                break;
            }
        }
    }

    private Vector3 GetCenteredWorldPosition(int x, int y, int width, int height)
    {
        float offsetX = (width - 1) * _cellSize / 2f;
        float offsetZ = (height - 1) * _cellSize / 2f;

        Vector3 localPosition = new Vector3(x * _cellSize - offsetX, 0, y * _cellSize - offsetZ);
        return transform.position + localPosition;
    }

    private int CalculateEarnedStars()
    {
        int stars = 0;

        if (!_hintsUsedThisLevel) stars++;

        if (_currentScore >= _currentLevel.TargetScoreForStar) stars++;

        return Mathf.Clamp(stars, 0, 3);
    }

    private IEnumerator AnimateStarsRoutine(int startIdx, int endIdx)
    {
        for (int i = startIdx; i < endIdx; i++)
        {
            GameObject star = _filledStarObjects[i];
            star.SetActive(true);

            star.transform.localScale = Vector3.zero;

            star.transform.DOScale(Vector3.one, _starAnimDuration).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(_starAnimDelay);
        }
    }

    private void UpdateInventoryUI()
    {
        foreach (var slot in _inventorySlots)
        {
            bool hasPiece = PlayerInventory.Contains(slot.Type);
            slot.SetUnlocked(hasPiece);
        }
    }

    private void ShowVictoryScreen()
    {
        _isAnimating = true;
        _victoryPanel.SetActive(true);

        bool isLastLevel = (_currentLevelIndex >= CampaignLevels.Count - 1);

        if (_nextLevelButton != null) _nextLevelButton.SetActive(!isLastLevel);
        if (_restartButton != null) _restartButton.SetActive(!isLastLevel);
        if (_cutsceneButton != null) _cutsceneButton.SetActive(isLastLevel);
        if (_mainMenuButton != null) _mainMenuButton.SetActive(isLastLevel);

        if (_levelProgressText != null && CampaignLevels != null)
        {
            if (isLastLevel)
            {
                _levelProgressText.text = "ИГРА ПРОЙДЕНА!";
            }
            else
            {
                _levelProgressText.text = $"УРОВЕНЬ {_currentLevelIndex + 1} ИЗ {CampaignLevels.Count}";
            }
        }

        int finalStars = CalculateEarnedStars() + 1;

        string levelKey = $"LevelProgress_{_currentLevel.name}";
        int previousStars = PlayerPrefs.GetInt(levelKey, 0);
        if (finalStars > previousStars)
        {
            PlayerPrefs.SetInt(levelKey, finalStars);
            PlayerPrefs.Save();
        }

        foreach (var star in _victoryStarObjects)
        {
            star.SetActive(false);
            star.transform.localScale = Vector3.zero;
        }

        StartCoroutine(AnimateVictoryStarsRoutine(finalStars));
    }

    public void UI_MainMenuButton()
    {
        _victoryPanel.SetActive(false);
        Time.timeScale = 1f;
        PauseAudioManager.StopSnapshot();
        AudioManager.StopAllPersistentAudio();
        SceneManager.LoadScene("StartGame");
    }

    public void UI_WatchCutsceneButton()
    {
        _victoryPanel.SetActive(false);
        Debug.Log("Запуск финальной катсцены...");
        int cutsceneSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (cutsceneSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(cutsceneSceneIndex);
        }
        else
        {
            Debug.LogError("Катсцена не найдена! Проверьте настройки сборки.");
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    private IEnumerator AnimateVictoryStarsRoutine(int count)
    {
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < count; i++)
        {
            GameObject star = _victoryStarObjects[i];
            star.SetActive(true);

            star.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void UI_NextLevelButton()
    {
        _victoryPanel.SetActive(false);
        LoadNextLevel();
    }

    public void UI_RestartButton()
    {
        _victoryPanel.SetActive(false);
        RestartLevel();
    }
}
