using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(SceneTransitionManager))]
public class SceneTransitionFmodAudio : MonoBehaviour
{
    [Header("FMOD Events")]
    [SerializeField] private EventReference _coverStarted;
    [SerializeField] private EventReference _coverCompleted;
    [SerializeField] private EventReference _sceneActivationStarted;
    [SerializeField] private EventReference _sceneActivated;
    [SerializeField] private EventReference _revealStarted;
    [SerializeField] private EventReference _revealCompleted;

    private void OnEnable()
    {
        SceneTransitionManager.PhaseChanged += OnTransitionPhaseChanged;
    }

    private void OnDisable()
    {
        SceneTransitionManager.PhaseChanged -= OnTransitionPhaseChanged;
    }

    private void OnTransitionPhaseChanged(SceneTransitionPhase phase)
    {
        switch (phase)
        {
            case SceneTransitionPhase.CoverStarted:
                Play(_coverStarted);
                break;
            case SceneTransitionPhase.CoverCompleted:
                Play(_coverCompleted);
                break;
            case SceneTransitionPhase.SceneActivationStarted:
                Play(_sceneActivationStarted);
                break;
            case SceneTransitionPhase.SceneActivated:
                Play(_sceneActivated);
                break;
            case SceneTransitionPhase.RevealStarted:
                Play(_revealStarted);
                break;
            case SceneTransitionPhase.RevealCompleted:
                Play(_revealCompleted);
                break;
        }
    }

    private void Play(EventReference eventReference)
    {
        if (!eventReference.IsNull)
        {
            RuntimeManager.PlayOneShot(eventReference);
        }
    }
}
