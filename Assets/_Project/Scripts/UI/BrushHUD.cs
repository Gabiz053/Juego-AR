// ??????????????????????????????????????????????
//  BrushHUD.cs  ·  _Project.Scripts.UI
//  UI controller for the Brush toggle button.
//  Subscribes to BrushTool and keeps the button visual in sync.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Mirrors the <see cref="BrushTool.IsBrushActive"/> state on the
    /// <c>Btn_Brush</c> button visual — dims when OFF, restores when ON.<br/>
    /// Follows the same pattern as <see cref="UndoRedoHUD"/>: a UI script
    /// holds a <c>[SerializeField]</c> reference to the service, subscribes
    /// to its event, and owns the visual update.<br/>
    /// Attach to <c>Btn_Brush</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Brush HUD")]
    public class BrushHUD : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Service")]
        [Tooltip("BrushTool that fires OnBrushToggled.")]
        [SerializeField] private BrushTool _brushTool;

        [Header("Visual")]
        [Tooltip("Image on the brush button — dimmed when brush is OFF. Auto-detected if left empty.")]
        [SerializeField] private Image _buttonImage;

        [Header("Visual State")]
        [Tooltip("Brightness multiplier when brush is OFF (0 = black, 1 = unchanged).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dimFactor = 0.45f;

        #endregion

        #region Internals ?????????????????????????????????????

        private Color _originalColor;
        private bool  _initialized;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (_brushTool != null)
                _brushTool.OnBrushToggled += RefreshVisual;
        }

        private void OnDisable()
        {
            if (_brushTool != null)
                _brushTool.OnBrushToggled -= RefreshVisual;
        }

        private void Start()
        {
            // Sync to initial state (brush starts OFF).
            if (_brushTool != null)
                RefreshVisual(_brushTool.IsBrushActive);
            else
                RefreshVisual(false);
        }

        #endregion

        #region Internals ?????????????????????????????????????

        private void RefreshVisual(bool isActive)
        {
            EnsureInitialized();
            if (_buttonImage == null) return;

            _buttonImage.color = isActive
                ? _originalColor
                : _originalColor * new Color(_dimFactor, _dimFactor, _dimFactor, 1f);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            if (_buttonImage == null)
                _buttonImage = GetComponent<Image>();

            if (_buttonImage != null)
                _originalColor = _buttonImage.color;

            _initialized = true;
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            if (_brushTool == null)
                Debug.LogWarning("[BrushHUD] _brushTool not assigned.", this);
        }

        #endregion
    }
}
