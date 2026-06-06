using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    [Header("Capture Feedback")]
    [SerializeField] private bool _enableCaptureFeedback = true;
    [SerializeField] private CaptureFeedbackVfx _captureFeedbackPrefab;
    [SerializeField] private float _captureFeedbackIntensity = 1f;
    [SerializeField] private float _captureEnemyShrinkDuration = 0.14f;
    [SerializeField] private float _captureEnemyPopScale = 1.08f;
    [SerializeField] private float _captureCameraImpulse = 0.16f;

    [Header("Capture Camera Impact")]
    [SerializeField, Min(0f)] private float _captureCameraShakeStrength = 0.14f;
    [SerializeField, Min(0f)] private float _captureCameraShakeDuration = 0.18f;
    [SerializeField, Min(0f)] private float _enemyCaptureCameraImpulse = 0.22f;
    [SerializeField, Min(0f)] private float _enemyCaptureCameraShakeStrength = 0.24f;
    [SerializeField, Min(0f)] private float _enemyCaptureCameraShakeDuration = 0.28f;
    [SerializeField, Min(0.1f)] private float _captureCameraShakeFrequency = 46f;

    [Header("Enemy Capture Restart")]
    [SerializeField, Min(0f)] private float _enemyCaptureRestartDelay = 1.2f;

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

    [Header("Cell Hover Lift")]
    [SerializeField] private float _cellHoverLiftHeight = 0.12f;
    [SerializeField] private float _cellHoverLiftDuration = 0.12f;

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
    private PieceView _currentHoveredPiece = null;
    private PieceView _currentLiftedPiece = null;
    private Vector2Int? _currentLiftedCell = null;
    private Tween _currentLiftedPieceTween = null;
    private PieceView _currentLiftedPieceTweenOwner = null;
    private Coroutine _enemyCaptureRestartRoutine = null;
    private int _victoryTextLocalizationVersion;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        _victoryTextLocalizationVersion++;
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void Start()
    {
        int requestedStartLevelIndex = TargetStartLevelIndex;
        TargetStartLevelIndex = -1;

        if (requestedStartLevelIndex >= 0 && CampaignLevels != null && requestedStartLevelIndex < CampaignLevels.Count)
        {
            _currentLevelIndex = requestedStartLevelIndex;
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

    private void OnSelectedLocaleChanged(Locale locale)
    {
        if (_victoryPanel != null && _victoryPanel.activeSelf)
        {
            UpdateVictoryProgressText();
        }
    }

    private void OnDestroy()
    {
        CancelEnemyCaptureRestartDelay();
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

        Vector3 worldPos = _cellViews[logicPos].BaseWorldPosition;
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
            ClearHover();
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
            PieceView targetPiece = null;

            if (hit.collider.TryGetComponent(out CellView hitCell))
            {
                targetCell = hitCell;
            }
            else if (hit.collider.TryGetComponent(out PieceView hitPiece))
            {
                targetPiece = hitPiece;

                if (_cellViews.TryGetValue(hitPiece.LogicPosition, out CellView pieceCell))
                {
                    targetCell = pieceCell;
                }
            }

            SetHoveredPiece(CanShowSelectablePieceHover(targetPiece) ? targetPiece : null);

            if (targetCell != null && targetCell != _currentHoveredCell)
            {
                ClearHoveredCell();
                _currentHoveredCell = targetCell;
                SetHoveredCellLift(_currentHoveredCell);

                if (_currentValidMoves.Contains(_currentHoveredCell.LogicPosition))
                {
                    _currentHoveredCell.SetValidDestinationHover(true);
                }
                else
                {
                    _currentHoveredCell.HighlightAsHover();
                }
            }
            else if (targetCell == null)
            {
                ClearHoveredCell();
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void ClearHover()
    {
        ClearHoveredCell();
        ClearPieceHover();
    }

    private void ClearHoveredCell()
    {
        if (_currentHoveredCell != null)
        {
            _currentHoveredCell.SetValidDestinationHover(false);
            ClearHoveredCellLift();

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

    private void SetHoveredPiece(PieceView piece)
    {
        if (piece == _currentHoveredPiece)
            return;

        ClearPieceHover();

        _currentHoveredPiece = piece;
        if (_currentHoveredPiece != null)
        {
            _currentHoveredPiece.SetSelectableHover(true);
        }
    }

    private void ClearPieceHover()
    {
        if (_currentHoveredPiece != null)
        {
            _currentHoveredPiece.SetSelectableHover(false);
        }

        _currentHoveredPiece = null;
    }

    private bool CanShowSelectablePieceHover(PieceView piece)
    {
        if (piece == null || piece.Alignment != Alignment.Player || piece == _selectedPiece)
            return false;

        return _pieceViews.TryGetValue(piece.LogicPosition, out PieceView registeredPiece) && registeredPiece == piece;
    }

    private bool ShouldPlayInvalidClickFeedback(BoardCell logicCell)
    {
        if (!logicCell.IsActive)
            return true;

        if (_selectedPiece != null)
            return true;

        return logicCell.HasEnemy;
    }

    private void PlayInvalidClickFeedback(Vector2Int pos)
    {
        if (_cellViews.TryGetValue(pos, out CellView cellView))
        {
            cellView.PlayInvalidClickFeedback();
        }
    }

    private void SetHoveredCellLift(CellView cellView)
    {
        Vector2Int pos = cellView.LogicPosition;

        cellView.SetHoverLift(true, _cellHoverLiftHeight, _cellHoverLiftDuration);
        _currentLiftedCell = pos;

        if (_pieceViews.TryGetValue(pos, out PieceView pieceView))
        {
            _currentLiftedPiece = pieceView;
            MovePieceForCellLift(pieceView, pos, true);
        }
    }

    private void ClearHoveredCellLift()
    {
        if (_currentLiftedCell.HasValue && _cellViews.TryGetValue(_currentLiftedCell.Value, out CellView cellView))
        {
            cellView.SetHoverLift(false, _cellHoverLiftHeight, _cellHoverLiftDuration);
        }

        if (_currentLiftedPiece != null && _currentLiftedCell.HasValue)
        {
            MovePieceForCellLift(_currentLiftedPiece, _currentLiftedCell.Value, false);
        }

        _currentLiftedCell = null;
        _currentLiftedPiece = null;
    }

    private void MovePieceForCellLift(PieceView pieceView, Vector2Int cellPos, bool isLifted)
    {
        if (!_cellViews.TryGetValue(cellPos, out CellView cellView))
            return;

        KillPieceLiftTween(pieceView);

        float targetY = cellView.BaseWorldPosition.y + 0.1f;
        if (pieceView == _selectedPiece)
        {
            targetY += _pieceHoverHeight;
        }

        if (isLifted)
        {
            targetY += _cellHoverLiftHeight;
        }

        _currentLiftedPieceTween = pieceView.transform
            .DOMoveY(targetY, _cellHoverLiftDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(pieceView.gameObject)
            .OnKill(() =>
            {
                if (_currentLiftedPieceTweenOwner == pieceView)
                {
                    _currentLiftedPieceTween = null;
                    _currentLiftedPieceTweenOwner = null;
                }
            });
        _currentLiftedPieceTweenOwner = pieceView;
    }

    private void KillPieceLiftTween(PieceView pieceView)
    {
        if (pieceView == null || _currentLiftedPieceTween == null || _currentLiftedPieceTweenOwner != pieceView)
            return;

        _currentLiftedPieceTween.Kill(false);
        _currentLiftedPieceTween = null;
        _currentLiftedPieceTweenOwner = null;
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
                        bool shouldPlayInvalidFeedback = ShouldPlayInvalidClickFeedback(logicCell);
                        DeselectPiece();

                        if (shouldPlayInvalidFeedback)
                        {
                            PlayInvalidClickFeedback(pos);
                        }
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
        ClearHover();

        if (_selectedPiece != null && _selectedPiece.LogicPosition == piecePos)
        {
            DeselectPiece();
            return;
        }

        DeselectPiece();

        _selectedPiece = _pieceViews[piecePos];
        _currentValidMoves = _engine.GetValidMoves(piecePos);
        KillPieceLiftTween(_selectedPiece);

        float targetY = _cellViews[piecePos].BaseWorldPosition.y + 0.1f + _pieceHoverHeight;
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
        ClearHover();

        if (_selectedPiece == null) return;

        if (_selectedPiece.gameObject != null)
        {
            KillPieceLiftTween(_selectedPiece);
            float baseY = _cellViews[_selectedPiece.LogicPosition].BaseWorldPosition.y + 0.1f;
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
        ClearHover();
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

                PlayCaptureFeedback(enemyPiece, toPos);
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

        Vector3 targetWorldPos = _cellViews[toPos].BaseWorldPosition;
        targetWorldPos.y += 0.1f;

        movingPiece.MoveToWorldPosition(targetWorldPos, () =>
        {
            if (isKingCaptured)
            {
                PlayCaptureCameraImpact(targetWorldPos, _captureCameraShakeStrength, _captureCameraShakeDuration, _captureCameraImpulse);
                AudioManager.PlayKingAngrySound();
                AudioManager.PlayWinSound();
                Debug.Log("КОРОЛЬ ПОВЕРЖЕН!");
                ShowVictoryScreen();
                return;
            }

            if (isCapture)
            {
                AudioManager.PlayCaptureSound(targetWorldPos);
                PlayCaptureCameraImpact(targetWorldPos, _captureCameraShakeStrength, _captureCameraShakeDuration, _captureCameraImpulse);
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

    private void PlayCaptureFeedback(PieceView enemyPiece, Vector2Int capturePos)
    {
        if (enemyPiece == null)
            return;

        Vector3 feedbackPos = enemyPiece.transform.position;
        if (_cellViews.TryGetValue(capturePos, out CellView cellView))
        {
            feedbackPos = cellView.BaseWorldPosition;
            feedbackPos.y += 0.1f;
        }

        if (_enableCaptureFeedback)
        {
            CaptureFeedbackVfx feedbackVfx;
            if (_captureFeedbackPrefab != null)
            {
                feedbackVfx = Instantiate(_captureFeedbackPrefab, feedbackPos, Quaternion.identity);
            }
            else
            {
                GameObject feedbackObject = new GameObject("Capture Feedback VFX");
                feedbackObject.transform.position = feedbackPos;
                feedbackVfx = feedbackObject.AddComponent<CaptureFeedbackVfx>();
            }

            feedbackVfx.Play(_captureFeedbackIntensity);
        }

        AnimateCapturedEnemy(enemyPiece);
    }

    private void PlayCaptureCameraImpact(Vector3 worldPosition, float shakeStrength, float shakeDuration, float impulseStrength)
    {
        if (_cameraController == null)
            return;

        _cameraController.AddImpulse(worldPosition, impulseStrength);
        _cameraController.AddShake(shakeStrength, shakeDuration, _captureCameraShakeFrequency);
    }

    private void AnimateCapturedEnemy(PieceView enemyPiece)
    {
        if (enemyPiece == null)
            return;

        foreach (Collider enemyCollider in enemyPiece.GetComponentsInChildren<Collider>())
        {
            enemyCollider.enabled = false;
        }

        Transform pieceTransform = enemyPiece.transform;
        pieceTransform.DOKill(false);

        Vector3 originalScale = pieceTransform.localScale;
        float shrinkDuration = Mathf.Max(0.01f, _captureEnemyShrinkDuration);

        Sequence captureSequence = DOTween.Sequence();
        captureSequence
            .SetLink(enemyPiece.gameObject)
            .Append(pieceTransform.DOScale(originalScale * _captureEnemyPopScale, 0.05f).SetEase(Ease.OutQuad))
            .Append(pieceTransform.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                if (enemyPiece != null)
                {
                    Destroy(enemyPiece.gameObject);
                }
            });
    }

    private void ExecuteEnemyRetaliation(Vector2Int enemyPos, Vector2Int playerPos, PieceView playerPiece)
    {
        Debug.Log("Враг наносит ответный удар!");

        Vector3 attackPos = _cellViews[playerPos].BaseWorldPosition;
        attackPos.y += 0.1f;
        AudioManager.PlayEnemyAttackSound(attackPos);

        _engine.MovePiece(enemyPos, playerPos);

        _engine.UpdateThreatMap();
        RefreshBoardThreats();

        PieceView retaliatingEnemy = _pieceViews[enemyPos];
        _pieceViews.Remove(enemyPos);
        _pieceViews[playerPos] = retaliatingEnemy;
        retaliatingEnemy.LogicPosition = playerPos;

        Vector3 targetWorldPos = _cellViews[playerPos].BaseWorldPosition;
        targetWorldPos.y += 0.1f;

        retaliatingEnemy.MoveToWorldPosition(targetWorldPos, () =>
        {
            Vector3 deathPos = _cellViews[playerPos].BaseWorldPosition;
            deathPos.y += 0.1f;

            AudioManager.PlayKingLaughSound();
            AudioManager.PlayKillSound(deathPos);
            AudioManager.PlayPlaceSound(targetWorldPos);

            PlayCaptureCameraImpact(deathPos, _enemyCaptureCameraShakeStrength, _enemyCaptureCameraShakeDuration, _enemyCaptureCameraImpulse);
            Destroy(playerPiece.gameObject);
            ScheduleEnemyCaptureRestart();
        });
    }

    private void ScheduleEnemyCaptureRestart()
    {
        CancelEnemyCaptureRestartDelay();
        _enemyCaptureRestartRoutine = StartCoroutine(RestartAfterEnemyCaptureDelay());
    }

    private IEnumerator RestartAfterEnemyCaptureDelay()
    {
        float delay = Mathf.Max(0f, _enemyCaptureRestartDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        _enemyCaptureRestartRoutine = null;
        RestartLevel();
    }

    private void CancelEnemyCaptureRestartDelay()
    {
        if (_enemyCaptureRestartRoutine == null)
            return;

        StopCoroutine(_enemyCaptureRestartRoutine);
        _enemyCaptureRestartRoutine = null;
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
        ClearHover();

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

        Vector3 worldPos = _cellViews[playerPos].BaseWorldPosition;
        worldPos.y += 0.1f;

        if (_morphParticlePrefab != null)
        {
            Vector3 particlePos = worldPos;
            particlePos.y += 0.1f;

            GameObject effect = Instantiate(_morphParticlePrefab, particlePos, Quaternion.identity);
            if (effect.TryGetComponent(out PieceTransformationVfx vfx))
            {
                vfx.Play();
            }
            else
            {
                Destroy(effect, 2f);
            }
        }

        Vector3 transformPos = _cellViews[playerPos].BaseWorldPosition;
        transformPos.y += 0.1f;
        AudioManager.PlayChangeSound(transformPos);

        PieceView oldView = _pieceViews[playerPos];
        oldView.transform.DOKill();

        Vector3 oldPieceScale = oldView.transform.localScale;
        Sequence oldPieceSequence = DOTween.Sequence();
        oldPieceSequence.Append(oldView.transform.DOScale(oldPieceScale * 1.08f, 0.06f).SetEase(Ease.OutQuad));
        oldPieceSequence.Append(oldView.transform.DOScale(Vector3.zero, 0.11f).SetEase(Ease.InBack));
        oldPieceSequence.OnComplete(() =>
        {
            if (oldView != null)
            {
                Destroy(oldView.gameObject);
            }
        });
        _pieceViews.Remove(playerPos);

        PieceView newView = Instantiate(newPrefab, worldPos, Quaternion.identity);
        Vector3 newPieceScale = newView.transform.localScale;
        newView.transform.localScale = Vector3.zero;

        Sequence newPieceSequence = DOTween.Sequence();
        newPieceSequence.SetDelay(0.08f);
        newPieceSequence.Append(newView.transform.DOScale(newPieceScale * 1.14f, 0.18f).SetEase(Ease.OutBack));
        newPieceSequence.Append(newView.transform.DOScale(newPieceScale, 0.08f).SetEase(Ease.OutQuad));
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
        ClearHover();

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
        CancelEnemyCaptureRestartDelay();
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
            Vector3 worldPos = _cellViews[playerLogicPos].BaseWorldPosition;
            _cameraController.TargetFocusPosition = worldPos;
        }
    }

    public void ToggleHints()
    {
        if (IsEditMode) return;

        ClearHover();

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

        UpdateVictoryProgressText();

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
        SceneTransitionManager.LoadScene("StartGame");
    }

    private void UpdateVictoryProgressText()
    {
        if (_levelProgressText == null || CampaignLevels == null)
            return;

        bool isLastLevel = (_currentLevelIndex >= CampaignLevels.Count - 1);

        if (isLastLevel)
        {
            int version = ++_victoryTextLocalizationVersion;
            _levelProgressText.text = "The Game Completed!";

            GameLocalization.GetStringAsync("state.game_complete", "The Game Completed!", localized =>
            {
                if (version != _victoryTextLocalizationVersion || _levelProgressText == null || !isActiveAndEnabled)
                    return;

                _levelProgressText.text = localized;
            });
            return;
        }

        int requestVersion = ++_victoryTextLocalizationVersion;
        int currentLevelNumber = _currentLevelIndex + 1;
        int levelCount = CampaignLevels.Count;

        _levelProgressText.text = $"LEVEL {currentLevelNumber} OF {levelCount}";

        GameLocalization.GetStringAsync("state.level", "LEVEL", level =>
        {
            if (requestVersion != _victoryTextLocalizationVersion || _levelProgressText == null || !isActiveAndEnabled)
                return;

            GameLocalization.GetStringAsync("state.from", "OF", from =>
            {
                if (requestVersion != _victoryTextLocalizationVersion || _levelProgressText == null || !isActiveAndEnabled)
                    return;

                _levelProgressText.text = $"{level} {currentLevelNumber} {from} {levelCount}";
            });
        });
    }

    public void UI_WatchCutsceneButton()
    {
        _victoryPanel.SetActive(false);
        Debug.Log("Запуск финальной катсцены...");
        int cutsceneSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (cutsceneSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneTransitionManager.LoadScene(cutsceneSceneIndex);
        }
        else
        {
            Debug.LogError("Катсцена не найдена! Проверьте настройки сборки.");
            SceneTransitionManager.LoadScene("StartGame");
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
