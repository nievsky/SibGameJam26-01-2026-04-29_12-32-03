using System.Collections;
using UnityEngine;
using FMODUnity;

public class MenuMusicStarter : MonoBehaviour
{
    [SerializeField] private StudioEventEmitter emitter;
    [SerializeField] private StudioEventEmitter emitter2;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait until FMOD fully initialized and banks loaded
        yield return new WaitUntil(() => RuntimeManager.HaveAllBanksLoaded);
        yield return new WaitForSeconds(0.1f); // small delay for safety

        if (emitter != null && !emitter.IsPlaying())
            emitter.Play();

        if (emitter2 != null && !emitter2.IsPlaying())
            emitter2.Play();
    }
}
