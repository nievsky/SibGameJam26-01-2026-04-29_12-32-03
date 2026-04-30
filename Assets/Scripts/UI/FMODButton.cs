using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FMODUnity;

[RequireComponent(typeof(Button))]
public class FMODButtonSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("FMOD Events")]
    [Tooltip("Sound played when the button is clicked.")]
    [SerializeField] private EventReference clickSoundEvent;

    [Tooltip("Sound played when the mouse hovers over the button.")]
    [SerializeField] private EventReference hoverSoundEvent;

    [Tooltip("If true, plays sounds in 3D (for world-space UI)")]
    [SerializeField] private bool use3DSound = false;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }

    /// <summary>
    /// Called automatically when the button is clicked.
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSoundEvent.IsNull) return;

        if (use3DSound)
            RuntimeManager.PlayOneShot(clickSoundEvent, transform.position);
        else
            RuntimeManager.PlayOneShot(clickSoundEvent);
    }

    /// <summary>
    /// Called automatically when the mouse pointer enters the button area.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable || hoverSoundEvent.IsNull) return;

        if (use3DSound)
            RuntimeManager.PlayOneShot(hoverSoundEvent, transform.position);
        else
            RuntimeManager.PlayOneShot(hoverSoundEvent);
    }
}
