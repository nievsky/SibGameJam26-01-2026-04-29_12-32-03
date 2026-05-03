using UnityEngine;
using System; // Нужно для работы с Array

[Serializable]
public class CellSetup
{
    public bool IsActive = true;
    public PieceType Piece = PieceType.None;
    public Alignment Alignment = Alignment.None;
}

[Serializable]
public class LevelRow
{
    // Убрали жесткую привязку к 8
    public CellSetup[] Columns; 
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "ChessGame/Level")]
public class LevelData : ScriptableObject
{
    [Header("Настройки уровня")]
    [Min(1)] public int Width = 8;  // [Min(1)] не даст поставить размер меньше 1
    [Min(1)] public int Height = 8;
    
    [Header("Условия (Очки для звезд)")]
    public int TargetScoreForStar = 10; // Очков для 1-й звезды

    [Header("Матрица поля (Разверни для настройки)")]
    public LevelRow[] Rows;

    // Этот метод вызывается Unity автоматически каждый раз, 
    // когда ты меняешь значения Width или Height в инспекторе
    private void OnValidate()
    {
        // 1. Изменяем размер массива строк (Height)
        if (Rows == null || Rows.Length != Height)
        {
            Array.Resize(ref Rows, Height);
        }

        // 2. Проходимся по каждой строке и меняем размер массива колонок (Width)
        for (int i = 0; i < Height; i++)
        {
            if (Rows[i] == null)
            {
                Rows[i] = new LevelRow();
            }

            if (Rows[i].Columns == null || Rows[i].Columns.Length != Width)
            {
                Array.Resize(ref Rows[i].Columns, Width);
            }

            // 3. Защита от NullReferenceException: 
            // Если массив увеличился, новые ячейки будут пустыми (null). Заполняем их базовыми настройками.
            for (int j = 0; j < Width; j++)
            {
                if (Rows[i].Columns[j] == null)
                {
                    Rows[i].Columns[j] = new CellSetup();
                }
            }
        }
    }
}