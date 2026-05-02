using UnityEngine;
using FMODUnity;

public static class AudioManager
{
    private static FMOD.Studio.EventInstance _musicInstance;
    private static string _currentMusicEvent = "";

    private static FMOD.Studio.EventInstance _ambienceInstance;
    private static string _currentAmbienceEvent = "";

    // --- ЗВУКОВЫЕ ЭФФЕКТЫ ---

    public static void PlayPickUpSound(Vector3 position)
    {
        Debug.Log($"Звук: Взятие фигуры в {position}");
        RuntimeManager.PlayOneShot("event:/chess take", position);
    }

    public static void PlayPlaceSound(Vector3 position)
    {
        Debug.Log($"Звук: Постановка фигуры в {position}");
        RuntimeManager.PlayOneShot("event:/chess place", position);
    }

    public static void PlayCaptureSound(Vector3 position)
    {
        Debug.Log($"Звук: Рубка в {position}");
        RuntimeManager.PlayOneShot("event:/chess kill", position);
    }

    public static void PlayEnemyAttackSound(Vector3 position)
    {
        Debug.Log($"Звук: Атака врага в {position}");
        RuntimeManager.PlayOneShot("event:/chess kill", position);
    }

    public static void PlayKillSound(Vector3 position)
    {
        Debug.Log($"Звук: Смерть игрока в {position}");
        RuntimeManager.PlayOneShot("event:/chess kill", position);
    }

    public static void PlayTransformationSound(Vector3 position)
    {
        Debug.Log($"Звук: Превращение в {position}");
        RuntimeManager.PlayOneShot("event:/chess place", position);
    }

    public static void PlayRestartSound(Vector3 position)
    {
        Debug.Log($"Звук: Рестарт в {position}");
    }

    // --- МУЗЫКА ---

    public static void PlayLevelMusic()
    {
        PlayMusic("event:/MusicLevel");
    }

    public static void PlayMusic(string eventPath)
    {
        if (_musicInstance.isValid())
        {
            if (_currentMusicEvent == eventPath)
            {
                Debug.Log("Музыка уже играет");
                return;
            }

            StopMusic();
        }

        _currentMusicEvent = eventPath;
        _musicInstance = RuntimeManager.CreateInstance(eventPath);
        _musicInstance.start();
        Debug.Log($"Запущена музыка: {eventPath}");
    }

    public static void StopMusic()
    {
        if (_musicInstance.isValid())
        {
            _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _musicInstance.release();
            Debug.Log("Музыка остановлена");
        }
    }

    public static void SetMusicParameter(string parameterName, float value)
    {
        if (_musicInstance.isValid())
        {
            _musicInstance.setParameterByName(parameterName, value);
        }
    }

    public static void SetMusicVolume(float volume)
    {
        if (_musicInstance.isValid())
        {
            _musicInstance.setVolume(volume);
        }
    }

    // --- ЭМБИЕНС ---

    public static void PlayAmbience()
    {
        PlayAmbience("event:/ambience");
    }

    public static void PlayAmbience(string eventPath)
    {
        if (_ambienceInstance.isValid())
        {
            if (_currentAmbienceEvent == eventPath)
            {
                Debug.Log("Эмбиенс уже играет");
                return;
            }

            StopAmbience();
        }

        _currentAmbienceEvent = eventPath;
        _ambienceInstance = RuntimeManager.CreateInstance(eventPath);
        _ambienceInstance.start();
        Debug.Log($"Запущен эмбиенс: {eventPath}");
    }

    public static void StopAmbience()
    {
        if (_ambienceInstance.isValid())
        {
            _ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _ambienceInstance.release();
            Debug.Log("Эмбиенс остановлен");
        }
    }

    public static void SetAmbienceParameter(string parameterName, float value)
    {
        if (_ambienceInstance.isValid())
        {
            _ambienceInstance.setParameterByName(parameterName, value);
        }
    }

    public static void SetAmbienceVolume(float volume)
    {
        if (_ambienceInstance.isValid())
        {
            _ambienceInstance.setVolume(volume);
        }
    }

    // --- МЕТОДЫ БЕЗ ПОЗИЦИИ (ДЛЯ УДОБСТВА) ---

    public static void PlayPickUpSound() => PlayPickUpSound(Vector3.zero);
    public static void PlayPlaceSound() => PlayPlaceSound(Vector3.zero);
    public static void PlayCaptureSound() => PlayCaptureSound(Vector3.zero);
    public static void PlayEnemyAttackSound() => PlayEnemyAttackSound(Vector3.zero);
    public static void PlayKillSound() => PlayKillSound(Vector3.zero);
    public static void PlayTransformationSound() => PlayTransformationSound(Vector3.zero);
    public static void PlayRestartSound() => PlayRestartSound(Vector3.zero);
}