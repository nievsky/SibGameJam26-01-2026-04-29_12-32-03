using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class BandPerformanceManager : MonoBehaviour
{
    [Header("FMOD Events")]
    [Tooltip("List of 'speaking' events (band chatter before each song).")]
    public List<EventReference> speakingEvents;

    [Tooltip("List of 'music' events (songs to play).")]
    public List<EventReference> musicEvents;

    private EventInstance currentEvent;

    void Start()
    {
        var menuMusic = GameObject.FindWithTag("MenuMusic");
        if (menuMusic != null)
        {
            Destroy(menuMusic);
        }

        StartCoroutine(PerformanceLoop());
    }

    private IEnumerator PerformanceLoop()
    {
        while (true)
        {
            // --- SPEAKING PART ---
            if (speakingEvents.Count > 0)
            {
                var speakingEvent = speakingEvents[Random.Range(0, speakingEvents.Count)];
                yield return PlayAndWaitForEvent(speakingEvent);
            }

            // --- MUSIC PART ---
            if (musicEvents.Count > 0)
            {
                var musicEvent = musicEvents[Random.Range(0, musicEvents.Count)];
                yield return PlayAndWaitForEvent(musicEvent);
            }
        }
    }

    private IEnumerator PlayAndWaitForEvent(EventReference eventRef)
    {
        currentEvent = RuntimeManager.CreateInstance(eventRef);


        currentEvent.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        currentEvent.start();


        bool isPlaying = true;
        while (isPlaying)
        {
            currentEvent.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.STOPPED || state == PLAYBACK_STATE.STOPPING)
            {
                isPlaying = false;
            }
            yield return null;
        }

        currentEvent.release();
    }
}