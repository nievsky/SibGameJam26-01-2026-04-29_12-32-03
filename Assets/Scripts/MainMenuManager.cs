using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Нужно для загрузки сцен

public class MainMenuManager : MonoBehaviour
{
    [Header("Настройки Кампании")]
    public List<LevelData> CampaignLevels; // Перетащи сюда те же уровни, что и в GameController
    public string GameSceneName = "GameScene"; // Точное название твоей сцены с игрой

    [Header("UI Элементы")]
    public Transform GridParent; // Куда будут спавниться кнопки (объект с GridLayoutGroup)
    public LevelSelectButtonUI ButtonPrefab; // Префаб кнопки уровня

    private void Start()
    {
        GenerateLevelGrid();
    }

    private void GenerateLevelGrid()
    {
        // 1. Очищаем сетку (на всякий случай, если там есть тестовые кнопки)
        foreach (Transform child in GridParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Проходимся по всем уровням кампании
        for (int i = 0; i < CampaignLevels.Count; i++)
        {
            LevelData level = CampaignLevels[i];
            
            // Читаем сколько звезд игрок сохранил для этого уровня
            string levelKey = $"LevelProgress_{level.name}";
            int earnedStars = PlayerPrefs.GetInt(levelKey, 0);

            // Создаем кнопку
            LevelSelectButtonUI newButton = Instantiate(ButtonPrefab, GridParent);
            
            // Чтобы внутри лямбда-выражения не сбился индекс, сохраняем его в локальную переменную
            int indexToLoad = i; 
            
            // Настраиваем кнопку: передаем номер, звезды и метод, который вызовется при клике
            newButton.Setup(indexToLoad, earnedStars, () => LoadGameSceneAtLevel(indexToLoad));
        }
    }

    private void LoadGameSceneAtLevel(int levelIndex)
    {
        Debug.Log($"Загружаем уровень {levelIndex + 1}...");
        
        // Передаем индекс в статическую переменную GameController'а
        GameController.TargetStartLevelIndex = levelIndex;
        
        // Загружаем игровую сцену
        SceneManager.LoadScene(GameSceneName);
    }
}