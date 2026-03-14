// ------------------------------------------------------------
//  UndoRedoHUD.cs  -  _Project.Scripts.UI
//  UI controller for the Undo / Redo buttons.
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Infrastructure;

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

        [Header("Buttons")]
        [Tooltip("Undo button -- dimmed when stack is empty.")]
        [SerializeField] private Button _undoButton;

        [Tooltip("Redo button -- dimmed when stack is empty.")]
        [SerializeField] private Button _redoButton;

        [Header("Icons")]
        [Tooltip("Image on the Undo button -- dimmed when unavailable.")]
        [SerializeField] private Image _undoIcon;

        [Tooltip("Image on the Redo button -- dimmed when unavailable.")]
        [SerializeField] private Image _redoIcon;

        [Header("Visual State")]
        [Tooltip("Opacity when the button is enabled.")]
        [Range(0f, 1f)]
        [SerializeField] private float _alphaEnabled  = 1.0f;

        [Tooltip("Opacity when the button is disabled.")]
        [Range(0f, 1f)]
        [SerializeField] private float _alphaDisabled = 0.35f;

        #endregion

        #region State ---------------------------------------------

        private IUndoRedoService _service;
        private IUIAudioService  _uiAudio;

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

        #region Unity Lifecycle -----------------------------------

        private void OnDisable()
        {
            if (_service != null)
                _service.OnStackChanged -= RefreshState;
        }

        private void Start()
        {
            ServiceLocator.TryGet<IUndoRedoService>(out _service);
            ServiceLocator.TryGet<IUIAudioService>(out _uiAudio);

            if (_service != null)
                _service.OnStackChanged += RefreshState;

            RefreshState(
                _service != null && _service.CanUndo,
                _service != null && _service.CanRedo);

            ValidateReferences();
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

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_service == null)
                Debug.LogWarning("[UndoRedoHUD] _service is not assigned.", this);
            if (_undoButton == null)
                Debug.LogWarning("[UndoRedoHUD] _undoButton is not assigned.", this);
            if (_redoButton == null)
                Debug.LogWarning("[UndoRedoHUD] _redoButton is not assigned.", this);
        }

        #endregion
    }
}
