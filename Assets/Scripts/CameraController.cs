using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Следование")]
    public Vector3 TargetFocusPosition; 
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 8f, -6f); 
    [SerializeField] private float _followSpeed = 5f;

    [Header("Обзор мышью от краев экрана")]
    public bool EnableEdgePeeking = true; 
    [SerializeField] private float _peekDistance = 3f; 
    [SerializeField] private float _edgeThreshold = 0.15f; 
    [SerializeField] private float _peekSpeed = 4f;

    [Header("Наклон камеры (Зажать ПКМ)")]
    public bool EnableTilt = true;
    [SerializeField] private float _tiltSensitivity = 2f;
    [SerializeField] private float _maxTiltAngle = 15f; 
    [SerializeField] private float _autoResetSpeed = 3f; 

    // Внутренние переменные для расчетов доски
    private Vector3 _boardCenter;
    private Vector2 _boardLimits;
    private bool _hasBoardData = false;

    private Vector3 _currentPeekOffset = Vector3.zero;
    private Vector2 _currentTilt = Vector2.zero; 

    private void Start()
    {
        transform.SetParent(null); 
    }

    // НОВЫЙ МЕТОД: Настройка лимитов камеры под текущий уровень
    // НОВЫЙ МЕТОД: Принимает готовый центр доски (centerPoint)
    public void SetupBoard(int width, int height, float cellSize, Vector3 centerPoint)
    {
        // 1. Центр доски теперь берем из контроллера
        _boardCenter = centerPoint;

        // 2. Рассчитываем рамки (насколько далеко камере разрешено отъезжать от центра)
        float limitX = Mathf.Max(0, (width * cellSize / 2f) - 2f);
        float limitZ = Mathf.Max(0, (height * cellSize / 2f) - 2f);
        _boardLimits = new Vector2(limitX, limitZ);

        // 3. Динамический зум. 
        float maxDim = Mathf.Max(width, height);
        float targetHeight = 7f + Mathf.Max(0, maxDim - 8) * 0.3f; 
        targetHeight = Mathf.Clamp(targetHeight, 7f, 13f); 

        _cameraOffset = new Vector3(0f, targetHeight, -targetHeight * 0.8f);
        _hasBoardData = true;
    }

    private void LateUpdate()
    {
        Vector3 targetPeekOffset = CalculateMousePeek();
        _currentPeekOffset = Vector3.Lerp(_currentPeekOffset, targetPeekOffset, Time.deltaTime * _peekSpeed);

        if (EnableTilt)
        {
            if (Input.GetMouseButton(1)) 
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                _currentTilt.y += mouseX * _tiltSensitivity;
                _currentTilt.x -= mouseY * _tiltSensitivity; 
                _currentTilt.x = Mathf.Clamp(_currentTilt.x, -_maxTiltAngle, _maxTiltAngle);
                _currentTilt.y = Mathf.Clamp(_currentTilt.y, -_maxTiltAngle, _maxTiltAngle);
            }
            else
            {
                _currentTilt = Vector2.Lerp(_currentTilt, Vector2.zero, Time.deltaTime * _autoResetSpeed);
            }
        }

        // --- УМНАЯ ЦЕЛЬ КАМЕРЫ ---
        Vector3 finalTarget = TargetFocusPosition;

        if (_hasBoardData)
        {
            // 1. Не даем цели выйти за пределы рассчитанных рамок доски
            finalTarget.x = Mathf.Clamp(finalTarget.x, _boardCenter.x - _boardLimits.x, _boardCenter.x + _boardLimits.x);
            finalTarget.z = Mathf.Clamp(finalTarget.z, _boardCenter.z - _boardLimits.y, _boardCenter.z + _boardLimits.y);
            
            // 2. Гравитация к центру: смешиваем позицию игрока и центр (80% игрок, 20% центр доски)
            finalTarget = Vector3.Lerp(finalTarget, _boardCenter, 0.2f);
        }

        Quaternion tiltRotation = Quaternion.Euler(_currentTilt.x, _currentTilt.y, 0f);
        Vector3 rotatedOffset = tiltRotation * _cameraOffset;

        // Едем к вычисленной цели
        Vector3 desiredCameraPosition = finalTarget + rotatedOffset + _currentPeekOffset;
        transform.position = Vector3.Lerp(transform.position, desiredCameraPosition, Time.deltaTime * _followSpeed);

        // Смотрим строго на вычисленную цель
        Vector3 lookTarget = finalTarget + _currentPeekOffset;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _followSpeed * 1.5f);
    }

    private Vector3 CalculateMousePeek()
    {
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

    public void SnapToTarget()
    {
        _currentPeekOffset = Vector3.zero;
        _currentTilt = Vector2.zero;
        
        // Для резкого прыжка тоже применяем ограничения
        Vector3 finalTarget = TargetFocusPosition;
        if (_hasBoardData)
        {
            finalTarget.x = Mathf.Clamp(finalTarget.x, _boardCenter.x - _boardLimits.x, _boardCenter.x + _boardLimits.x);
            finalTarget.z = Mathf.Clamp(finalTarget.z, _boardCenter.z - _boardLimits.y, _boardCenter.z + _boardLimits.y);
            finalTarget = Vector3.Lerp(finalTarget, _boardCenter, 0.2f);
        }

        transform.position = finalTarget + _cameraOffset;
        transform.rotation = Quaternion.LookRotation(finalTarget - transform.position);
    }
}