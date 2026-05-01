using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Prefabs & Setup")]
    [SerializeField] private CellView _cellPrefab;
    [SerializeField] private PieceView _knightPrefab; // Для примера возьмем префаб Коня
    [SerializeField] private float _cellSize = 1.1f;  // Расстояние между 3D клетками
    [SerializeField] private LayerMask _cellLayer;    // Слой, на котором находятся клетки (для Raycast)

    private ChessEngine _engine;
    private Dictionary<Vector2Int, CellView> _cellViews = new Dictionary<Vector2Int, CellView>();
    private Dictionary<Vector2Int, PieceView> _pieceViews = new Dictionary<Vector2Int, PieceView>();

    // Состояние инпута
    private PieceView _selectedPiece = null;
    private List<Vector2Int> _currentValidMoves = new List<Vector2Int>();
    private bool _isAnimating = false;

    private void Start()
    {
        GenerateBoard(8, 8);
        SpawnPlayerPiece(new Vector2Int(0, 0));
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

    private void Update()
    {
        if (_isAnimating) return; // Блокируем инпут во время анимации хода

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
            // Убираем ограничение по _cellLayer, чтобы луч мог попадать и по самим фигурам
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // 1. Проверяем, кликнули ли мы прямо по 3D-модели фигуры
                if (hit.collider.TryGetComponent(out PieceView clickedPiece))
                {
                    if (clickedPiece.Alignment == Alignment.Player)
                    {
                        SelectPiece(clickedPiece.LogicPosition);
                        return; // Фигура выделена, прерываем логику
                    }
                }

                // 2. Если попали по клетке (или пошли на неё после выделения)
                if (hit.collider.TryGetComponent(out CellView clickedCell))
                {
                    Vector2Int pos = clickedCell.LogicPosition;
                    BoardCell logicCell = _engine.GetCell(pos);

                    // Если фигура УЖЕ выбрана и мы кликаем по подсвеченной клетке — делаем ход
                    if (_selectedPiece != null && _currentValidMoves.Contains(pos))
                    {
                        ExecuteMove(_selectedPiece.LogicPosition, pos);
                    }
                    // На случай, если игрок кликнул именно по клетке под своей фигурой, а не по самой фигуре
                    else if (logicCell.HasPlayer)
                    {
                        SelectPiece(pos);
                    }
                    else
                    {
                        // Кликнули по недоступной клетке — сбрасываем выделение
                        DeselectPiece();
                    }
                }
            }
            else
            {
                // Кликнули вообще мимо доски
                DeselectPiece();
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
        _isAnimating = true; // Блокируем клики

        // 1. Двигаем в логике
        _engine.MovePiece(fromPos, toPos);

        // 2. Обновляем словари визуала
        PieceView movingPiece = _pieceViews[fromPos];
        _pieceViews.Remove(fromPos);
        _pieceViews[toPos] = movingPiece;
        movingPiece.LogicPosition = toPos;

        // 3. Анимируем визуал
        Vector3 targetWorldPos = _cellViews[toPos].transform.position;
        targetWorldPos.y += 0.5f; // Офсет по высоте

        movingPiece.MoveToWorldPosition(targetWorldPos, () => 
        {
            _isAnimating = false; // Разблокируем инпут, когда анимация закончится
            // Здесь в будущем можно будет передать ход врагам
        });
    }
}