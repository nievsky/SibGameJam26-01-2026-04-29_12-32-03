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

    public List<Vector2Int> GetValidMoves(Vector2Int startPos)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        BoardCell startCell = GetCell(startPos);

        if (startCell == null || startCell.IsEmpty || !startCell.HasPlayer)
            return validMoves;

        switch (startCell.CurrentPiece)
        {
            case PieceType.Rook:
                CalculateLinearMoves(startPos, validMoves, new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right });
                break;
            case PieceType.Bishop:
                CalculateLinearMoves(startPos, validMoves, new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) });
                break;
            case PieceType.Knight:
                CalculateKnightMoves(startPos, validMoves);
                break;
        }

        return validMoves;
    }

    // Расчет ходов по линиям (для Ладьи и Слона)
    private void CalculateLinearMoves(Vector2Int startPos, List<Vector2Int> validMoves, Vector2Int[] directions)
    {
        foreach (Vector2Int dir in directions)
        {
            Vector2Int currentPos = startPos + dir;

            while (IsWithinBounds(currentPos.x, currentPos.y))
            {
                BoardCell cell = GetCell(currentPos);

                // Если клетка пассивна (стена) - луч прерывается
                if (!cell.IsActive) break;

                if (cell.IsEmpty)
                {
                    validMoves.Add(currentPos); // Пустая клетка - можно идти
                }
                else if (cell.HasEnemy)
                {
                    validMoves.Add(currentPos); // Враг - можно бить
                    break;                      // Дальше врага пройти нельзя
                }
                else if (cell.HasPlayer)
                {
                    break; // Своя фигура блокирует путь
                }

                currentPos += dir;
            }
        }
    }

    // Расчет ходов для Коня (прыжки)
    private void CalculateKnightMoves(Vector2Int startPos, List<Vector2Int> validMoves)
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
                
                // Конь может перепрыгивать препятствия, но конечная клетка должна быть активна
                // и на ней не должно быть союзной фигуры
                if (cell.IsActive && !cell.HasPlayer)
                {
                    validMoves.Add(targetPos);
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
