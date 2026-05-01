using UnityEngine;

[System.Serializable]
public class CellSetup
{
    public bool IsActive = true; // false = пропасть/стена
    public PieceType Piece = PieceType.None;
    public Alignment Alignment = Alignment.None;
}

[System.Serializable]
public class LevelRow
{
    // Массив из 8 клеток (одна горизонтальная линия доски)
    public CellSetup[] Columns = new CellSetup[8];
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "ChessGame/Level")]
public class LevelData : ScriptableObject
{
    [Header("Настройки уровня")]
    public int Width = 8;
    public int Height = 8;

    [Header("Матрица поля (Разверни элементы для настройки)")]
    // Элемент 0 - это Y=0 (низ доски), Элемент 7 - это Y=7 (верх доски)
    public LevelRow[] Rows = new LevelRow[8]; 
}