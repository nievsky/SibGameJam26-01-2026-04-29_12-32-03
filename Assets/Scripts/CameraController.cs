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

    [Header("Camera Impulse")]
    [SerializeField] private float _impulseDamping = 12f;
    [SerializeField] private float _impulseReturnSpeed = 18f;
    [SerializeField] private float _maxImpulseOffset = 0.35f;

    [Header("Camera Shake")]
    [SerializeField] private float _maxShakeOffset = 0.45f;

    // Внутренние переменные для расчетов доски
    private Vector3 _boardCenter;
    private Vector2 _boardLimits;
    private bool _hasBoardData = false;

    private Vector3 _currentPeekOffset = Vector3.zero;
    private Vector2 _currentTilt = Vector2.zero; 
    private Vector3 _impulseOffset = Vector3.zero;
    private Vector3 _impulseVelocity = Vector3.zero;
    private Vector3 _smoothedCameraPosition = Vector3.zero;
    private bool _hasSmoothedCameraPosition = false;
    private Vector3 _shakeOffset = Vector3.zero;
    private Vector2 _shakeSeed = Vector2.zero;
    private float _shakeTimer = 0f;
    private float _shakeDuration = 0f;
    private float _shakeStrength = 0f;
    private float _shakeFrequency = 30f;

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
        UpdateImpulseOffset();
        UpdateShakeOffset();

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

        Vector3 desiredCameraPosition = finalTarget + rotatedOffset + _currentPeekOffset;
        if (!_hasSmoothedCameraPosition)
        {
            _smoothedCameraPosition = transform.position - _impulseOffset - _shakeOffset;
            _hasSmoothedCameraPosition = true;
        }

        _smoothedCameraPosition = Vector3.Lerp(_smoothedCameraPosition, desiredCameraPosition, Time.deltaTime * _followSpeed);
        transform.position = _smoothedCameraPosition + _impulseOffset + _shakeOffset;

        // Смотрим строго на вычисленную цель
        Vector3 lookTarget = finalTarget + _currentPeekOffset + _impulseOffset * 0.35f + _shakeOffset * 0.65f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _followSpeed * 1.5f);
    }

    public void AddImpulse(Vector3 worldPosition, float strength)
    {
        if (strength <= 0f)
            return;

        Vector3 direction = transform.position - worldPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.right;
        }

        Vector3 impulse = direction.normalized * strength;
        impulse += Vector3.up * (strength * 0.25f);
        _impulseVelocity += impulse;
        _impulseVelocity = Vector3.ClampMagnitude(_impulseVelocity, _maxImpulseOffset * _impulseDamping);
    }

    public void AddShake(float strength, float duration, float frequency)
    {
        if (strength <= 0f || duration <= 0f)
            return;

        _shakeStrength = Mathf.Max(_shakeStrength, strength);
        _shakeDuration = Mathf.Max(_shakeDuration, duration);
        _shakeTimer = Mathf.Max(_shakeTimer, duration);
        _shakeFrequency = Mathf.Max(0.1f, frequency);
        _shakeSeed = Random.insideUnitCircle * 100f;
    }

    private void UpdateImpulseOffset()
    {
        if (_impulseOffset.sqrMagnitude < 0.000001f && _impulseVelocity.sqrMagnitude < 0.000001f)
            return;

        _impulseOffset += _impulseVelocity * Time.deltaTime;
        _impulseOffset = Vector3.ClampMagnitude(_impulseOffset, _maxImpulseOffset);
        _impulseVelocity = Vector3.Lerp(_impulseVelocity, Vector3.zero, Time.deltaTime * _impulseDamping);
        _impulseOffset = Vector3.Lerp(_impulseOffset, Vector3.zero, Time.deltaTime * _impulseReturnSpeed);
    }

    private void UpdateShakeOffset()
    {
        if (_shakeTimer <= 0f)
        {
            _shakeOffset = Vector3.zero;
            _shakeDuration = 0f;
            _shakeStrength = 0f;
            return;
        }

        _shakeTimer = Mathf.Max(0f, _shakeTimer - Time.deltaTime);

        float normalizedTime = _shakeDuration > 0f ? _shakeTimer / _shakeDuration : 0f;
        float envelope = normalizedTime * normalizedTime;
        float time = Time.time * _shakeFrequency;

        float x = (Mathf.PerlinNoise(_shakeSeed.x, time) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(_shakeSeed.y, time + 19.31f) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(_shakeSeed.x + 41.17f, _shakeSeed.y + time) - 0.5f) * 2f;

        Vector3 rawShake = transform.right * x + transform.up * (y * 0.45f) + transform.forward * (z * 0.25f);
        _shakeOffset = Vector3.ClampMagnitude(rawShake, 1f) * Mathf.Min(_shakeStrength, _maxShakeOffset) * envelope;
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
        _impulseOffset = Vector3.zero;
        _impulseVelocity = Vector3.zero;
        _shakeOffset = Vector3.zero;
        _shakeTimer = 0f;
        _shakeDuration = 0f;
        _shakeStrength = 0f;
        _hasSmoothedCameraPosition = true;
        
        // Для резкого прыжка тоже применяем ограничения
        Vector3 finalTarget = TargetFocusPosition;
        if (_hasBoardData)
        {
            finalTarget.x = Mathf.Clamp(finalTarget.x, _boardCenter.x - _boardLimits.x, _boardCenter.x + _boardLimits.x);
            finalTarget.z = Mathf.Clamp(finalTarget.z, _boardCenter.z - _boardLimits.y, _boardCenter.z + _boardLimits.y);
            finalTarget = Vector3.Lerp(finalTarget, _boardCenter, 0.2f);
        }

        _smoothedCameraPosition = finalTarget + _cameraOffset;
        transform.position = _smoothedCameraPosition;
        transform.rotation = Quaternion.LookRotation(finalTarget - transform.position);
    }
}
