using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Следование")]
    public Vector3 TargetFocusPosition; 
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 8f, -6f); 
    [SerializeField] private float _followSpeed = 5f;

    [Header("Обзор мышью (Edge Peeking)")]
    public bool EnableEdgePeeking = true; // <-- Наш новый чекбокс
    [SerializeField] private float _peekDistance = 3f; 
    [SerializeField] private float _edgeThreshold = 0.15f; 
    [SerializeField] private float _peekSpeed = 4f;

    private Vector3 _currentPeekOffset = Vector3.zero;

    private void Start()
    {
        transform.SetParent(null); 
    }

    private void LateUpdate()
    {
        Vector3 targetPeekOffset = CalculateMousePeek();
        _currentPeekOffset = Vector3.Lerp(_currentPeekOffset, targetPeekOffset, Time.deltaTime * _peekSpeed);

        Vector3 desiredCameraPosition = TargetFocusPosition + _cameraOffset + _currentPeekOffset;
        transform.position = Vector3.Lerp(transform.position, desiredCameraPosition, Time.deltaTime * _followSpeed);
    }

    private Vector3 CalculateMousePeek()
    {
        // Если чекбокс выключен - никакого смещения не происходит
        if (!EnableEdgePeeking) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        Vector3 peek = Vector3.zero;

        if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
            return peek;

        if (mousePos.x < Screen.width * _edgeThreshold)
        {
            float intensity = 1f - (mousePos.x / (Screen.width * _edgeThreshold));
            peek.x = -_peekDistance * intensity;
        }
        else if (mousePos.x > Screen.width * (1f - _edgeThreshold))
        {
            float intensity = (mousePos.x - Screen.width * (1f - _edgeThreshold)) / (Screen.width * _edgeThreshold);
            peek.x = _peekDistance * intensity;
        }

        if (mousePos.y < Screen.height * _edgeThreshold)
        {
            float intensity = 1f - (mousePos.y / (Screen.height * _edgeThreshold));
            peek.z = -_peekDistance * intensity;
        }
        else if (mousePos.y > Screen.height * (1f - _edgeThreshold))
        {
            float intensity = (mousePos.y - Screen.height * (1f - _edgeThreshold)) / (Screen.height * _edgeThreshold);
            peek.z = _peekDistance * intensity;
        }

        return peek;
    }

    // --- НОВЫЙ МЕТОД: Мгновенный перенос камеры ---
    public void SnapToTarget()
    {
        _currentPeekOffset = Vector3.zero;
        transform.position = TargetFocusPosition + _cameraOffset;
    }
}