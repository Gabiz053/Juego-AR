// ------------------------------------------------------------
//  DropdownButtonState.cs  -  _Project.Scripts.UI
//  Reflects an on/off toggle state on a dropdown button's visuals.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Dims or restores a toggle button's background colour to show
    /// ON/OFF state.  Attach to each toggleable button inside the
    /// options dropdown.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Dropdown Button State")]
    public class DropdownButtonState : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Visuals")]
        [Tooltip("Image whose colour is dimmed when OFF (auto-detected if empty).")]
        [SerializeField] private Image _buttonBackground;

        [Header("OFF State")]
        [Tooltip("Brightness multiplier when disabled (0 = black, 1 = unchanged).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dimFactor = 0.4f;

        #endregion

        #region State ---------------------------------------------

        private Color _originalColor;
        private bool  _initialized;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()    => EnsureInitialized();
        private void OnEnable() => EnsureInitialized();

        #endregion

        #region Public API ----------------------------------------

        /// <summary>ON = original colour, OFF = darkened.</summary>
        public void SetState(bool isOn)
        {
            EnsureInitialized();
            if (_buttonBackground == null) return;

            _buttonBackground.color = isOn
                ? _originalColor
                : _originalColor * new Color(_dimFactor, _dimFactor, _dimFactor, 1f);
        }

        #endregion

        #region Internals -----------------------------------------

        private void EnsureInitialized()
        {
            if (_initialized) return;

            if (_buttonBackground == null)
                _buttonBackground = GetComponent<Image>();
            if (_buttonBackground != null)
                _originalColor = _buttonBackground.color;

            _initialized = true;
        }

        #endregion
    }
}
