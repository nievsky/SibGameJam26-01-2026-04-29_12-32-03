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

    private RendererState[] _rendererStates;
    private Vector3 _baseScale;
    private Tween _hoverTween;
    private bool _isSelectableHovered;

    private void Awake()
    {
        _baseScale = transform.localScale;
        CacheRendererStates();
    }

    private void OnDisable()
    {
        ClearSelectableHover(true);
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
