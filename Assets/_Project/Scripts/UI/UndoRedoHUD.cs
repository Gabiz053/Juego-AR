// ------------------------------------------------------------
//  UndoRedoHUD.cs  -  _Project.Scripts.UI
//  UI controller for the Undo / Redo buttons.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Subscribes to <see cref="UndoRedoService"/> and keeps the
    /// Undo / Redo buttons in sync.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Undo Redo HUD")]
    public class UndoRedoHUD : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Service")]
        [Tooltip("UndoRedoService that this HUD reflects.")]
        [SerializeField] private UndoRedoService _service;

        [Header("Buttons")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _redoButton;

        [Header("Icons")]
        [Tooltip("Image on the Undo button -- dimmed when unavailable.")]
        [SerializeField] private Image _undoIcon;

        [Tooltip("Image on the Redo button -- dimmed when unavailable.")]
        [SerializeField] private Image _redoIcon;

        [Header("Visual State")]
        [Range(0f, 1f)]
        [SerializeField] private float _alphaEnabled  = 1.0f;

        [Range(0f, 1f)]
        [SerializeField] private float _alphaDisabled = 0.35f;

        [Header("Audio")]
        [Tooltip("UI audio service (auto-located from MainCanvas if empty).")]
        [SerializeField] private UIAudioService _uiAudio;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void OnEnable()
        {
            if (_service != null)
                _service.OnStackChanged += RefreshState;
        }

        private void OnDisable()
        {
            if (_service != null)
                _service.OnStackChanged -= RefreshState;
        }

        private void Start()
        {
            if (_uiAudio == null)
            {
                Canvas root = GetComponentInParent<Canvas>();
                if (root != null)
                    _uiAudio = root.GetComponentInChildren<UIAudioService>();
            }

            RefreshState(
                _service != null && _service.CanUndo,
                _service != null && _service.CanRedo);
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Called by Btn_Undo.onClick.</summary>
        public void OnUndoPressed()
        {
            _uiAudio?.PlayClick();
            _service?.Undo();
        }

        /// <summary>Called by Btn_Redo.onClick.</summary>
        public void OnRedoPressed()
        {
            _uiAudio?.PlayClick();
            _service?.Redo();
        }

        #endregion

        #region Internals -----------------------------------------

        private void RefreshState(bool canUndo, bool canRedo)
        {
            SetButtonState(_undoButton, _undoIcon, canUndo);
            SetButtonState(_redoButton, _redoIcon, canRedo);
        }

        private void SetButtonState(Button btn, Image icon, bool enabled)
        {
            if (btn != null) btn.interactable = enabled;

            if (icon != null)
            {
                Color c = icon.color;
                c.a        = enabled ? _alphaEnabled : _alphaDisabled;
                icon.color = c;
            }
        }

        #endregion
    }
}
