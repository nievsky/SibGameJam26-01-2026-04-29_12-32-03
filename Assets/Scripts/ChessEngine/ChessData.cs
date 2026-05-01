using UnityEngine;

// Тип фигуры
public enum PieceType 
{ 
    None, 
    Rook,   // Ладья
    Bishop, // Слон
    Knight  // Конь
}

// Принадлежность фигуры
public enum Alignment 
{ 
    None, 
    Player, 
    Enemy 
}

// Логическая клетка
public class BoardCell
{
    public Vector2Int Position { get; private set; }
    
    // Активна ли клетка (если false - это стена/пропасть)
    public bool IsActive { get; set; } = true; 
    
    public PieceType CurrentPiece { get; set; } = PieceType.None;
    public Alignment PieceAlignment { get; set; } = Alignment.None;

    public BoardCell(int x, int y)
    {
        Position = new Vector2Int(x, y);
    }

    public bool IsEmpty => CurrentPiece == PieceType.None;
    public bool HasEnemy => PieceAlignment == Alignment.Enemy;
    public bool HasPlayer => PieceAlignment == Alignment.Player;

    // Очистка клетки (например, после ухода фигуры или смерти врага)
    public void ClearPiece()
    {
        CurrentPiece = PieceType.None;
        PieceAlignment = Alignment.None;
    }
}
