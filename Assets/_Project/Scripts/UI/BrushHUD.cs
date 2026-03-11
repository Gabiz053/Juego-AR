// ------------------------------------------------------------
//  BrushHUD.cs  -  _Project.Scripts.UI
//  UI controller for the Brush toggle button visual.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Mirrors <see cref="BrushTool.IsBrushActive"/> on the Btn_Brush
    /// button visual -- dims when OFF, restores when ON.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Brush HUD")]
    public class BrushHUD : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Service")]
        [Tooltip("BrushTool that fires OnBrushToggled.")]
        [SerializeField] private BrushTool _brushTool;

        [Header("Visual")]
        [Tooltip("Image on the brush button (auto-detected if empty).")]
        [SerializeField] private Image _buttonImage;

        [Tooltip("Brightness multiplier when brush is OFF.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dimFactor = 0.45f;

        #endregion

        #region State ---------------------------------------------

        private Color _originalColor;
        private bool  _initialized;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()  => EnsureInitialized();

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
            RefreshVisual(_brushTool != null && _brushTool.IsBrushActive);
        }

        #endregion

        #region Internals -----------------------------------------

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
    }
}
