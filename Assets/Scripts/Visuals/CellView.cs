using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public class CellView : MonoBehaviour
{
    public Vector2Int LogicPosition { get; private set; }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private Vector3 _baseScale;
    private Vector3 _basePosition;
    private Tween _destinationHoverTween;
    private Tween _invalidClickTween;
    private Tween _hoverLiftTween;
    private int _colorPropertyId;
    private Color _baseColor = Color.white;
    private bool _isDestinationHovered;

    [Header("State Textures")]
    [SerializeField] private Texture2D _normalTexture;
    [SerializeField] private Texture2D _inactiveTexture;
    [SerializeField] private Texture2D _moveTexture;
    [SerializeField] private Texture2D _attackTexture;
    [SerializeField] private Texture2D _threatTexture;
    [SerializeField] private Texture2D _hoverTexture;

    [SerializeField] private string _shaderTextureName = "_BaseMap";

    [Header("Valid Destination Hover")]
    [SerializeField] private float _destinationHoverScale = 1.06f;
    [SerializeField] private float _destinationHoverPulseDuration = 0.32f;
    [SerializeField] private float _destinationHoverExitDuration = 0.12f;
    [SerializeField] private Color _destinationHoverTint = new Color(1f, 0.9f, 0.35f, 1f);
    [SerializeField, Range(0f, 1f)] private float _destinationHoverTintStrength = 0.45f;

    [Header("Invalid Click Feedback")]
    [SerializeField] private float _invalidClickDuration = 0.18f;
    [SerializeField] private float _invalidClickPunchScale = 0.045f;
    [SerializeField] private Color _invalidClickTint = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField, Range(0f, 1f)] private float _invalidClickTintStrength = 0.7f;

    public bool IsActiveCell { get; private set; }
    public bool IsThreatened { get; set; } = false;
    public Vector3 BaseWorldPosition => _basePosition;

    public void Init(Vector2Int logicPos, Texture2D baseTexture, bool isActive)
    {
        LogicPosition = logicPos;
        IsActiveCell = isActive;

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _baseScale = transform.localScale;
        _basePosition = transform.position;

        Material material = _renderer.sharedMaterial;
        _colorPropertyId = GetColorPropertyId(material);
        if (_colorPropertyId != 0)
        {
            _baseColor = material.GetColor(_colorPropertyId);
        }

        _normalTexture = baseTexture;

        ResetHighlight();
    }

    private void OnDisable()
    {
        SetHoverLift(false, 0f, 0f, true);
        KillInvalidClickTween();
        SetValidDestinationHover(false, true);
    }

    private void ApplyTexture(Texture2D tex)
    {
        if (tex == null) return;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture(_shaderTextureName, tex);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void SetActiveState(bool isActive, Texture2D baseTexture)
    {
        IsActiveCell = isActive;
        SetHoverLift(false, 0f, 0f, true);
        SetValidDestinationHover(false, true);

        if (baseTexture != null)
        {
            _normalTexture = baseTexture;
        }

        ResetHighlight();
    }

    public void HighlightAsMove() => ApplyTexture(_moveTexture);
    public void HighlightAsAttack() => ApplyTexture(_attackTexture);
    public void HighlightAsHover() => ApplyTexture(_hoverTexture);

    public void ResetHighlight()
    {
        if (IsThreatened)
        {
            ApplyTexture(_threatTexture);
        }
        else
        {
            ApplyTexture(IsActiveCell ? _normalTexture : _inactiveTexture);
        }
    }

    public void SetValidDestinationHover(bool isHovered)
    {
        SetValidDestinationHover(isHovered, false);
    }

    public void SetHoverLift(bool isLifted, float liftHeight, float duration)
    {
        SetHoverLift(isLifted, liftHeight, duration, false);
    }

    private void SetHoverLift(bool isLifted, float liftHeight, float duration, bool immediate)
    {
        KillHoverLiftTween();

        float targetY = _basePosition.y + (isLifted ? liftHeight : 0f);

        if (immediate || !gameObject.activeInHierarchy || duration <= 0f)
        {
            Vector3 position = transform.position;
            position.y = targetY;
            transform.position = position;
            return;
        }

        _hoverLiftTween = transform
            .DOMoveY(targetY, duration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject)
            .OnKill(() => _hoverLiftTween = null);
    }

    public void PlayInvalidClickFeedback()
    {
        KillInvalidClickTween();

        if (!_isDestinationHovered)
        {
            KillDestinationHoverTween();
            transform.localScale = _baseScale;
        }

        Vector3 punchScale = _baseScale * _invalidClickPunchScale;

        Sequence sequence = DOTween.Sequence().SetLink(gameObject);
        sequence.AppendCallback(() => ApplyColorTint(_invalidClickTint, _invalidClickTintStrength));
        sequence.Join(transform.DOPunchScale(punchScale, _invalidClickDuration, 8, 0.65f));
        sequence.AppendCallback(RestoreCurrentTint);
        sequence.OnKill(() => _invalidClickTween = null);

        _invalidClickTween = sequence;
    }

    private void SetValidDestinationHover(bool isHovered, bool immediate)
    {
        if (isHovered == _isDestinationHovered && _destinationHoverTween == null)
            return;

        _isDestinationHovered = isHovered;

        KillDestinationHoverTween();
        ApplyDestinationHoverTint(isHovered);

        if (isHovered)
        {
            _destinationHoverTween = transform
                .DOScale(_baseScale * _destinationHoverScale, _destinationHoverPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }
        else if (immediate || !gameObject.activeInHierarchy)
        {
            transform.localScale = _baseScale;
        }
        else
        {
            _destinationHoverTween = transform
                .DOScale(_baseScale, _destinationHoverExitDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
    }

    private void KillDestinationHoverTween()
    {
        if (_destinationHoverTween == null)
            return;

        _destinationHoverTween.Kill(false);
        _destinationHoverTween = null;
    }

    private void KillInvalidClickTween()
    {
        if (_invalidClickTween == null)
            return;

        _invalidClickTween.Kill(false);
        _invalidClickTween = null;
        RestoreCurrentTint();
    }

    private void KillHoverLiftTween()
    {
        if (_hoverLiftTween == null)
            return;

        _hoverLiftTween.Kill(false);
        _hoverLiftTween = null;
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

    private void ApplyDestinationHoverTint(bool isHovered)
    {
        if (isHovered)
        {
            ApplyColorTint(_destinationHoverTint, _destinationHoverTintStrength);
        }
        else
        {
            ApplyBaseColor();
        }
    }

    private void RestoreCurrentTint()
    {
        ApplyDestinationHoverTint(_isDestinationHovered);
    }

    private void ApplyColorTint(Color tint, float strength)
    {
        if (_colorPropertyId == 0)
            return;

        Color color = Color.Lerp(_baseColor, tint, strength);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_colorPropertyId, color);
        _renderer.SetPropertyBlock(_propBlock);
    }

    private void ApplyBaseColor()
    {
        if (_colorPropertyId == 0)
            return;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_colorPropertyId, _baseColor);
        _renderer.SetPropertyBlock(_propBlock);
    }
}
