using System.Collections.Generic;
using UnityEngine;

public class ChessEngine
{
    private BoardCell[,] _grid;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public ChessEngine(int width = 8, int height = 8)
    {
        Width = width;
        Height = height;
        _grid = new BoardCell[Width, Height];

        // Инициализация пустой доски
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _grid[x, y] = new BoardCell(x, y);
            }
        }
    }

    // Получить клетку по координатам
    public BoardCell GetCell(int x, int y)
    {
        if (IsWithinBounds(x, y))
            return _grid[x, y];
        return null;
    }

    public BoardCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

    // Проверка, не вышли ли мы за пределы доски
    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    // --- ЛОГИКА РАСЧЕТА ХОДОВ ---

    public List<Vector2Int> GetValidMoves(Vector2Int startPos, bool isForThreatMap = false)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        BoardCell startCell = GetCell(startPos);

        if (startCell == null || startCell.IsEmpty)
            return validMoves;

        Alignment alignment = startCell.PieceAlignment; // Смотрим, чья это фигура

        switch (startCell.CurrentPiece)
        {
            case PieceType.Rook:
                CalculateLinearMoves(startPos, validMoves, new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }, alignment, isForThreatMap);
                break;
            case PieceType.Bishop:
                CalculateLinearMoves(startPos, validMoves, new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) }, alignment, isForThreatMap);
                break;
            case PieceType.Knight:
                CalculateKnightMoves(startPos, validMoves, alignment);
                break;
            case PieceType.Queen:
                CalculateLinearMoves(startPos, validMoves, new Vector2Int[] { 
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) 
                }, alignment, isForThreatMap);
                break;
            case PieceType.King:
                CalculateKingMoves(startPos, validMoves, alignment);
                break;
            case PieceType.Pawn:
                CalculatePawnMoves(startPos, validMoves, alignment, isForThreatMap);
                break;
        }

        return validMoves;
    }

    // Расчет ходов по линиям (для Ладьи и Слона)
   private void CalculateLinearMoves(Vector2Int startPos, List<Vector2Int> validMoves, Vector2Int[] directions, Alignment alignment, bool isForThreatMap)
    {
        foreach (Vector2Int dir in directions)
        {
            Vector2Int currentPos = startPos + dir;
            Vector2Int? lastValidEmptyCell = null;

            while (IsWithinBounds(currentPos.x, currentPos.y))
            {
                BoardCell cell = GetCell(currentPos);
                
                // Уперлись в стену
                if (!cell.IsActive) 
                {
                    if (!isForThreatMap && lastValidEmptyCell.HasValue) 
                        validMoves.Add(lastValidEmptyCell.Value);
                    break;
                }

                if (cell.IsEmpty)
                {
                    lastValidEmptyCell = currentPos;
                    if (isForThreatMap) validMoves.Add(currentPos); 
                }
                else if (cell.PieceAlignment != alignment) 
                {
                    // Уперлись в противника (срубаем его)
                    validMoves.Add(currentPos); 
                    break; 
                }
                else if (cell.PieceAlignment == alignment) 
                {
                    // Уперлись в СВОЕГО союзника (другого врага)
                    if (isForThreatMap) 
                    {
                        // Если рисуем угрозы - ЭТА КЛЕТКА ТОЖЕ ПОД УДАРОМ/ЗАЩИТОЙ!
                        validMoves.Add(currentPos); 
                    }
                    else if (lastValidEmptyCell.HasValue) 
                    {
                        validMoves.Add(lastValidEmptyCell.Value);
                    }
                    break; // В любом случае луч останавливается, сквозь союзников стрелять нельзя
                }

                currentPos += dir;
            }

            if (!isForThreatMap && !IsWithinBounds(currentPos.x, currentPos.y) && lastValidEmptyCell.HasValue)
            {
                validMoves.Add(lastValidEmptyCell.Value);
            }
        }
    }
    
    

    // Расчет ходов для Коня (прыжки)
    private void CalculateKnightMoves(Vector2Int startPos, List<Vector2Int> validMoves, Alignment alignment, bool isForThreatMap = false)
    {
        Vector2Int[] knightOffsets = new Vector2Int[]
        {
            new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(1, -2),
            new Vector2Int(-1, -2), new Vector2Int(-2, -1), new Vector2Int(-2, 1), new Vector2Int(-1, 2)
        };

        foreach (Vector2Int offset in knightOffsets)
        {
            Vector2Int targetPos = startPos + offset;
            if (IsWithinBounds(targetPos.x, targetPos.y))
            {
                BoardCell cell = GetCell(targetPos);
                if (cell.IsActive)
                {
                    // Если рисуем карту угроз — конь бьет по этой клетке независимо от того, кто там стоит
                    if (isForThreatMap) 
                    {
                        validMoves.Add(targetPos);
                    }
                    // Если это ход — идем только туда, где нет союзников
                    else if (cell.PieceAlignment != alignment) 
                    {
                        validMoves.Add(targetPos);
                    }
                }
            }
        }
    }
    
    private void CalculateKingMoves(Vector2Int startPos, List<Vector2Int> validMoves, Alignment alignment, bool isForThreatMap = false)
    {
        Vector2Int[] kingDirections = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int dir in kingDirections)
        {
            Vector2Int targetPos = startPos + dir;
            if (IsWithinBounds(targetPos.x, targetPos.y))
            {
                BoardCell cell = GetCell(targetPos);
                if (cell.IsActive)
                {
                    if (isForThreatMap) 
                    {
                        validMoves.Add(targetPos);
                    }
                    else if (cell.PieceAlignment != alignment)
                    {
                        validMoves.Add(targetPos);
                    }
                }
            }
        }
    }

 private void CalculatePawnMoves(Vector2Int startPos, List<Vector2Int> validMoves, Alignment alignment, bool isForThreatMap)
    {
        // Игрок идет вверх (y + 1), враг идет вниз (y - 1)
        int forwardDirection = (alignment == Alignment.Player) ? 1 : -1;
        
        // 1. Движение вперед (Только если мы НЕ считаем карту угроз, потому что пешка не бьет вперед)
        if (!isForThreatMap)
        {
            Vector2Int forwardPos = startPos + new Vector2Int(0, forwardDirection);
            if (IsWithinBounds(forwardPos.x, forwardPos.y))
            {
                BoardCell forwardCell = GetCell(forwardPos);
                // Пешка идет вперед ТОЛЬКО если клетка активна и там никого нет
                if (forwardCell.IsActive && forwardCell.IsEmpty)
                {
                    validMoves.Add(forwardPos);
                }
            }
        }

        // 2. Атака по диагонали
        Vector2Int[] attackOffsets = new Vector2Int[] { new Vector2Int(-1, forwardDirection), new Vector2Int(1, forwardDirection) };
        foreach (Vector2Int offset in attackOffsets)
        {
            Vector2Int attackPos = startPos + offset;
            if (IsWithinBounds(attackPos.x, attackPos.y))
            {
                BoardCell attackCell = GetCell(attackPos);
                
                if (isForThreatMap)
                {
                    // Если мы составляем карту угроз врагов, мы просто добавляем диагонали
                    // (даже если они пустые, чтобы игрок знал, что туда наступать нельзя)
                    if (attackCell.IsActive) 
                        validMoves.Add(attackPos);
                }
                else
                {
                    // Если это расчет хода игрока, мы добавляем диагональ ТОЛЬКО если там стоит враг, которого можно срубить
                    if (attackCell.IsActive && !attackCell.IsEmpty && attackCell.PieceAlignment != alignment)
                    {
                        validMoves.Add(attackPos);
                    }
                }
            }
        }
    }
    
    public void UpdateThreatMap()
    {
        // 1. Очищаем старые угрозы
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _grid[x, y].AttackedBy.Clear(); // Очищаем список
            }
        }

        // 2. Ищем всех врагов и записываем ИХ координаты в клетки, куда они бьют
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                BoardCell cell = _grid[x, y];
                if (cell.HasEnemy)
                {
                    List<Vector2Int> threats = GetValidMoves(cell.Position, true);
                    foreach (Vector2Int threatPos in threats)
                    {
                        // Записываем позицию врага (cell.Position) в клетку под угрозой
                        GetCell(threatPos).AttackedBy.Add(cell.Position); 
                    }
                }
            }
        }
    }

    // Выполнение хода в логике
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        BoardCell cellFrom = GetCell(from);
        BoardCell cellTo = GetCell(to);

        if (cellFrom != null && cellTo != null && !cellFrom.IsEmpty)
        {
            // Копируем данные фигуры на новую клетку
            cellTo.CurrentPiece = cellFrom.CurrentPiece;
            cellTo.PieceAlignment = cellFrom.PieceAlignment;
            
            // Очищаем старую
            cellFrom.ClearPiece();
        }
    }
}
