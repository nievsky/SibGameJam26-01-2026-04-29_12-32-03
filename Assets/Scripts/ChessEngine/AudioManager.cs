using UnityEngine;
using FMODUnity;

public static class AudioManager
{
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

    public static void PlayPickUpSound() => PlayPickUpSound(Vector3.zero);
    public static void PlayPlaceSound() => PlayPlaceSound(Vector3.zero);
    public static void PlayCaptureSound() => PlayCaptureSound(Vector3.zero);
    public static void PlayEnemyAttackSound() => PlayEnemyAttackSound(Vector3.zero);
    public static void PlayKillSound() => PlayKillSound(Vector3.zero);
    public static void PlayTransformationSound() => PlayTransformationSound(Vector3.zero);
    public static void PlayRestartSound() => PlayRestartSound(Vector3.zero);
}