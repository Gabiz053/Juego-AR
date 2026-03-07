// ??????????????????????????????????????????????
//  DropdownButtonState.cs  ·  _Project.Scripts.UI
//  Reflects an on/off toggle state on a dropdown button's visuals.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Dims or restores a toggle button's background colour to show ON/OFF state.<br/>
    /// The button label text is never modified — only the background <see cref="Image"/>
    /// colour changes: ON = original colour, OFF = original colour darkened by
    /// <see cref="_dimFactor"/>.<br/>
    /// Attach to each toggleable <c>Btn_X</c> inside <c>Panel_OptionsDropdown</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Dropdown Button State")]
    public class DropdownButtonState : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Visuals")]
        [Tooltip("Image component on this button whose color is dimmed when OFF. Auto-detected if left empty.")]
        [SerializeField] private Image _buttonBackground;

        [Header("OFF state")]
        [Tooltip("Brightness multiplier applied to the original colour when DISABLED (0 = black, 1 = unchanged). 0.4 = clearly dimmed.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dimFactor = 0.4f;

        #endregion

        #region Internals ?????????????????????????????????????

        private Color _originalColor;
        private bool  _initialized;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()   => EnsureInitialized();
        private void OnEnable() => EnsureInitialized();

        private void Start()
        {
            if (_buttonBackground == null)
                Debug.LogWarning("[DropdownButtonState] _buttonBackground not found — dimming will not work.", this);
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// ON  ? restores the original button colour.<br/>
        /// OFF ? darkens the colour by <see cref="_dimFactor"/>.<br/>
        /// Label text is never touched.
        /// </summary>
        public void SetState(bool isOn)
        {
            EnsureInitialized();

            if (_buttonBackground == null) return;

            _buttonBackground.color = isOn
                ? _originalColor
                : _originalColor * new Color(_dimFactor, _dimFactor, _dimFactor, 1f);

            Debug.Log($"[DropdownButtonState] '{name}' ? {(isOn ? "ON" : "OFF")}.");
        }

        #endregion

        #region Internals ?????????????????????????????????????

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
