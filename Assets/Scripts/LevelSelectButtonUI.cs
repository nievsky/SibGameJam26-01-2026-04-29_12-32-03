using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LevelSelectButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _levelText; // Текст "1", "2" и т.д.
    [SerializeField] private GameObject[] _filledStars; // 3 желтые звезды (заполненные)

    public void Setup(int levelIndex, int earnedStars, UnityAction onButtonClicked)
    {
        // Устанавливаем номер уровня (index начинается с 0, поэтому +1)
        if (_levelText != null)
        {
            _levelText.text = (levelIndex + 1).ToString();
        }

        // Включаем нужное количество звезд
        for (int i = 0; i < _filledStars.Length; i++)
        {
            _filledStars[i].SetActive(i < earnedStars);
        }

        // Вешаем действие на кнопку
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onButtonClicked);
        }
    }
}