using UnityEngine;
using DG.Tweening;

public class PieceView : MonoBehaviour
{
    public PieceType Type;
    public Alignment Alignment;
    public Vector2Int LogicPosition { get; set; }

    [Header("Animation Settings")]
    [SerializeField] private float _moveDuration = 0.4f;
    [SerializeField] private float _jumpHeight = 1f;

    [Header("Selectable Hover Feedback")]
    [SerializeField] private float _selectableHoverScale = 1.08f;
    [SerializeField] private float _selectableHoverPulseDuration = 0.42f;
    [SerializeField] private float _selectableHoverExitDuration = 0.12f;
    [SerializeField] private Color _selectableHoverTint = new Color(1f, 0.82f, 0.35f, 1f);
    [SerializeField, Range(0f, 1f)] private float _selectableHoverTintStrength = 0.35f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Transform _dragBodyRoot;
    private RendererState[] _rendererStates;
    private Vector3 _baseScale;
    private Quaternion _dragBodyBaseLocalRotation;
    private Vector3 _dragGrabPivotLocalPoint;
    private Tween _hoverTween;
    private Tween _dragBodyTween;
    private bool _isSelectableHovered;
    private bool _hasDragGrabPivot;

    private void Awake()
    {
        _baseScale = transform.localScale;
        CacheRendererStates();
    }

    private void OnDisable()
    {
        ClearSelectableHover(true);
        KillDragBodyTween();

        Transform bodyRoot = GetDragBodyRoot();
        bodyRoot.localRotation = _dragBodyBaseLocalRotation;
        _hasDragGrabPivot = false;
    }

    public void MoveToWorldPosition(Vector3 targetWorldPos, System.Action onComplete = null)
    {
        transform.DOJump(targetWorldPos, _jumpHeight, 1, _moveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void SetSelectableHover(bool isHovered)
    {
        if (isHovered == _isSelectableHovered)
            return;

        if (isHovered)
        {
            StartSelectableHover();
        }
        else
        {
            ClearSelectableHover(false);
        }
    }

    public void ResetInteractionFeedback()
    {
        ClearSelectableHover(true);
    }

    public void BeginDragBodyInertia(float grabPivotHeightRatio)
    {
        Transform bodyRoot = GetDragBodyRoot();
        _dragBodyBaseLocalRotation = bodyRoot.localRotation;
        _dragGrabPivotLocalPoint = CalculateDragGrabPivotLocalPoint(bodyRoot, grabPivotHeightRatio);
        _hasDragGrabPivot = true;

        KillDragBodyTween();
    }

    public Vector3 GetDragGrabPivotWorldPosition()
    {
        Transform bodyRoot = GetDragBodyRoot();
        if (!_hasDragGrabPivot)
        {
            _dragGrabPivotLocalPoint = CalculateDragGrabPivotLocalPoint(bodyRoot, 1f);
            _hasDragGrabPivot = true;
        }

        return bodyRoot.TransformPoint(_dragGrabPivotLocalPoint);
    }

    public Vector3 GetDragRootPositionForGrabPoint(Vector3 grabWorldPosition)
    {
        Vector3 rootToGrabPivot = GetDragGrabPivotWorldPosition() - transform.position;
        return grabWorldPosition - rootToGrabPivot;
    }

    public void UpdateDragBodyInertia(Vector3 worldVelocity, float maxTiltAngle, float smoothing)
    {
        Transform bodyRoot = GetDragBodyRoot();

        worldVelocity.y = 0f;
        if (worldVelocity.sqrMagnitude < 0.0001f || maxTiltAngle <= 0f)
        {
            bodyRoot.localRotation = Quaternion.Slerp(
                bodyRoot.localRotation,
                _dragBodyBaseLocalRotation,
                1f - Mathf.Exp(-smoothing * Time.deltaTime));
            return;
        }

        float speed01 = Mathf.Clamp01(worldVelocity.magnitude / 12f);
        Vector3 localDirection = transform.InverseTransformDirection(worldVelocity.normalized);
        Quaternion targetRotation = _dragBodyBaseLocalRotation * Quaternion.Euler(
            localDirection.z * maxTiltAngle * speed01,
            0f,
            -localDirection.x * maxTiltAngle * speed01);

        bodyRoot.localRotation = Quaternion.Slerp(
            bodyRoot.localRotation,
            targetRotation,
            1f - Mathf.Exp(-smoothing * Time.deltaTime));
    }

    public void EndDragBodyInertia(float duration)
    {
        Transform bodyRoot = GetDragBodyRoot();

        KillDragBodyTween();
        if (duration <= 0f || !gameObject.activeInHierarchy)
        {
            bodyRoot.localRotation = _dragBodyBaseLocalRotation;
            return;
        }

        _dragBodyTween = bodyRoot
            .DOLocalRotateQuaternion(_dragBodyBaseLocalRotation, duration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject)
            .OnKill(() => _dragBodyTween = null);
    }

    private void StartSelectableHover()
    {
        _isSelectableHovered = true;

        KillHoverTween();
        ApplyHoverTint(true);

        _hoverTween = transform
            .DOScale(_baseScale * _selectableHoverScale, _selectableHoverPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void ClearSelectableHover(bool immediate)
    {
        if (!_isSelectableHovered && _hoverTween == null)
            return;

        _isSelectableHovered = false;

        KillHoverTween();
        ApplyHoverTint(false);

        if (immediate || !gameObject.activeInHierarchy)
        {
            transform.localScale = _baseScale;
        }
        else
        {
            _hoverTween = transform
                .DOScale(_baseScale, _selectableHoverExitDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
    }

    private void KillHoverTween()
    {
        if (_hoverTween == null)
            return;

        _hoverTween.Kill(false);
        _hoverTween = null;
    }

    private Transform GetDragBodyRoot()
    {
        if (_dragBodyRoot != null)
            return _dragBodyRoot;

        if (TryGetComponent(out Renderer _))
        {
            _dragBodyRoot = transform;
        }
        else
        {
            Renderer childRenderer = GetComponentInChildren<Renderer>(true);
            _dragBodyRoot = childRenderer != null
                ? childRenderer.transform
                : transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        _dragBodyBaseLocalRotation = _dragBodyRoot.localRotation;
        return _dragBodyRoot;
    }

    private Vector3 CalculateDragGrabPivotLocalPoint(Transform bodyRoot, float grabPivotHeightRatio)
    {
        float ratio = Mathf.Clamp01(grabPivotHeightRatio);
        Vector3 pivotWorldPosition = transform.position;

        if (TryGetRendererBounds(out Bounds bounds))
        {
            float rootToTop = Mathf.Max(0.01f, bounds.max.y - transform.position.y);
            pivotWorldPosition.y = transform.position.y + rootToTop * ratio;
        }

        return bodyRoot.InverseTransformPoint(pivotWorldPosition);
    }

    private bool TryGetRendererBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void KillDragBodyTween()
    {
        if (_dragBodyTween == null)
            return;

        _dragBodyTween.Kill(false);
        _dragBodyTween = null;
    }

    private void CacheRendererStates()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        _rendererStates = new RendererState[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer.sharedMaterial;
            int colorPropertyId = GetColorPropertyId(material);
            Color baseColor = colorPropertyId == 0 ? Color.white : material.GetColor(colorPropertyId);

            _rendererStates[i] = new RendererState(renderer, colorPropertyId, baseColor);
        }
    }

    private static int GetColorPropertyId(Material material)
    {
        if (material == null)
            return 0;

        if (material.HasProperty(BaseColorId))
            return BaseColorId;

        if (material.HasProperty(ColorId))
            return ColorId;

        return 0;
    }

    private void ApplyHoverTint(bool isHovered)
    {
        if (_rendererStates == null)
            CacheRendererStates();

        foreach (RendererState state in _rendererStates)
        {
            state.ApplyTint(isHovered, _selectableHoverTint, _selectableHoverTintStrength);
        }
    }

    private sealed class RendererState
    {
        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _propertyBlock;
        private readonly int _colorPropertyId;
        private readonly Color _baseColor;

        public RendererState(Renderer renderer, int colorPropertyId, Color baseColor)
        {
            _renderer = renderer;
            _colorPropertyId = colorPropertyId;
            _baseColor = baseColor;
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void ApplyTint(bool isHovered, Color hoverTint, float tintStrength)
        {
            if (_renderer == null || _colorPropertyId == 0)
                return;

            Color color = isHovered ? Color.Lerp(_baseColor, hoverTint, tintStrength) : _baseColor;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_colorPropertyId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
