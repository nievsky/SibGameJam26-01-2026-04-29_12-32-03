using UnityEngine;
using UnityEngine.UI;
using FMOD.Studio;
using FMODUnity;

public class FMODVolumeSlider : MonoBehaviour
{
    [Header("FMOD Bus Paths")]
    [Tooltip("FMOD Bus path, e.g. bus:/MusicBUS or bus:/SoundsBUS")]
    [SerializeField] private string busPath = "bus:/";

    [Header("UI Reference")]
    [Tooltip("Slider controlling the bus volume (0–1 range).")]
    [SerializeField] private Slider slider;

    private Bus fmodBus;
    private const string PREF_KEY_PREFIX = "FMODVolume_";

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // Initialize FMOD Bus
        fmodBus = RuntimeManager.GetBus(busPath);

        // Load saved volume (if any)
        float savedVolume = PlayerPrefs.GetFloat(PREF_KEY_PREFIX + busPath, 1f);
        slider.value = savedVolume;

        // Apply it to FMOD
        SetVolume(savedVolume);

        // Listen for slider changes
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(SetVolume);
    }

    private void SetVolume(float value)
    {
        if (fmodBus.isValid())
        {
            fmodBus.setVolume(value);
        }

        // Save to PlayerPrefs
        PlayerPrefs.SetFloat(PREF_KEY_PREFIX + busPath, value);
    }
}
