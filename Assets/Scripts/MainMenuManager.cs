using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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

    private int _lastTotalStars;
    private int _lastMaxStars;
    private int _rankLocalizationVersion;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        _rankLocalizationVersion++;
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void Start()
    {
        GenerateLevelGridAndCalculateRank();
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        UpdateRankUI(_lastTotalStars, _lastMaxStars);
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

        _lastTotalStars = totalEarnedStars;
        _lastMaxStars = maxPossibleStars;

        // Обновляем UI ранга
        UpdateRankUI(totalEarnedStars, maxPossibleStars);
    }

    private void UpdateRankUI(int totalStars, int maxStars)
    {
        // Устанавливаем звание
        if (RankText != null)
        {
            UpdateRankText(totalStars);
        }

        // Опционально: показываем цифры (например, "12 / 39")
        if (TotalStarsText != null)
        {
            TotalStarsText.text = $"{totalStars} / {maxStars}";
        }
    }

    // Метод, который определяет звание по количеству звезд
    private void UpdateRankText(int stars)
    {
        int version = ++_rankLocalizationVersion;
        string key = GetRankKey(stars);
        string fallback = GetRankFallback(stars);

        RankText.text = fallback;

        GameLocalization.GetStringAsync(key, fallback, localized =>
        {
            if (version != _rankLocalizationVersion || RankText == null || !isActiveAndEnabled)
                return;

            RankText.text = localized;
        });
    }

    private string GetRankKey(int stars)
    {
        if (stars == 0) return "rank.jester";
        if (stars >= 1 && stars <= 10) return "rank.debutant";
        if (stars >= 11 && stars <= 20) return "rank.castling_fan";
        if (stars >= 21 && stars <= 30) return "rank.pawn_grandmaster";
        if (stars >= 31 && stars <= 38) return "rank.king_of_metamorphoses";
        if (stars >= 39) return "rank.lavender_raf";
        
        return "rank.unknown";
    }

    private string GetRankFallback(int stars)
    {
        if (stars == 0) return "Jester";
        if (stars >= 1 && stars <= 10) return "Debutant";
        if (stars >= 11 && stars <= 20) return "A Castling Fan";
        if (stars >= 21 && stars <= 30) return "Pawn Grandmaster";
        if (stars >= 31 && stars <= 38) return "The King of Metamorphoses";
        if (stars >= 39) return "Lavender Raf";

        return "Unknown Rank";
    }

    private void LoadGameSceneAtLevel(int levelIndex)
    {
        Debug.Log($"Загружаем уровень {levelIndex + 1}...");
        GameController.TargetStartLevelIndex = levelIndex;
        SceneManager.LoadScene(GameSceneName);
    }
}
