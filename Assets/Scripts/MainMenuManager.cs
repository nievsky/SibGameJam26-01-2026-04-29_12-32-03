using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class MainMenuManager : MonoBehaviour
{
    [Header("Настройки Кампании")]
    public List<LevelData> CampaignLevels; 
    public string GameSceneName = "GameScene"; 

    [Header("UI Сетки Уровней")]
    public Transform GridParent; 
    public LevelSelectButtonUI ButtonPrefab; 

    [Header("UI Прогресса и Ранга")]
    public TextMeshProUGUI RankText; // Текст для звания (Шут, Дебютант и т.д.)
    public TextMeshProUGUI TotalStarsText; // (Опционально) Текст для показа суммы звезд "Звезды: 15 / 39"

    private void Start()
    {
        GenerateLevelGridAndCalculateRank();
    }

    private void GenerateLevelGridAndCalculateRank()
    {
        foreach (Transform child in GridParent)
        {
            Destroy(child.gameObject);
        }

        int totalEarnedStars = 0;
        int maxPossibleStars = CampaignLevels.Count * 3;

        for (int i = 0; i < CampaignLevels.Count; i++)
        {
            LevelData level = CampaignLevels[i];
            
            string levelKey = $"LevelProgress_{level.name}";
            int earnedStars = PlayerPrefs.GetInt(levelKey, 0);

            // Плюсуем звезды текущего уровня к общей сумме
            totalEarnedStars += earnedStars;

            LevelSelectButtonUI newButton = Instantiate(ButtonPrefab, GridParent);
            int indexToLoad = i; 
            newButton.Setup(indexToLoad, earnedStars, () => LoadGameSceneAtLevel(indexToLoad));
        }

        // Обновляем UI ранга
        UpdateRankUI(totalEarnedStars, maxPossibleStars);
    }

    private void UpdateRankUI(int totalStars, int maxStars)
    {
        // Устанавливаем звание
        if (RankText != null)
        {
            RankText.text = GetRankName(totalStars);
        }

        // Опционально: показываем цифры (например, "12 / 39")
        if (TotalStarsText != null)
        {
            TotalStarsText.text = $"{totalStars} / {maxStars}";
        }
    }

    // Метод, который определяет звание по количеству звезд
    private string GetRankName(int stars)
    {
        if (stars == 0) return "Шут";
        if (stars >= 1 && stars <= 10) return "Жертва дебюта / Дебютант";
        if (stars >= 11 && stars <= 20) return "Любитель рокировок";
        if (stars >= 21 && stars <= 30) return "Гроссмейстер пешек";
        if (stars >= 31 && stars <= 38) return "Король метаморфоз";
        if (stars >= 39) return "Лавандовый Раф";
        
        return "Неизвестный ранг";
    }

    private void LoadGameSceneAtLevel(int levelIndex)
    {
        Debug.Log($"Загружаем уровень {levelIndex + 1}...");
        GameController.TargetStartLevelIndex = levelIndex;
        SceneManager.LoadScene(GameSceneName);
    }
}