// ??????????????????????????????????????????????
//  UndoRedoHUD.cs  ·  _Project.Scripts.UI
//  UI controller for the Undo / Redo buttons.
//  Subscribes to UndoRedoService and keeps buttons in sync.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Undo Redo HUD")]
    public class UndoRedoHUD : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Service")]
        [Tooltip("UndoRedoService that this HUD reflects.")]
        [SerializeField] private UndoRedoService _service;

        [Header("Buttons")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _redoButton;

        [Header("Icons")]
        [Tooltip("Image on the Undo button — dimmed when unavailable.")]
        [SerializeField] private Image _undoIcon;

        [Tooltip("Image on the Redo button — dimmed when unavailable.")]
        [SerializeField] private Image _redoIcon;

        [Header("Visual State")]
        [Range(0f, 1f)]
        [SerializeField] private float _alphaEnabled  = 1.0f;

        [Range(0f, 1f)]
        [SerializeField] private float _alphaDisabled = 0.35f;

        [Header("Audio")]
        [Tooltip("UI audio service — plays click on undo/redo. Auto-located from MainCanvas if left empty.")]
        [SerializeField] private UIAudioService _uiAudio;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

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
            // Auto-locate UIAudioService on MainCanvas if not manually assigned.
            if (_uiAudio == null)
            {
                Canvas root = GetComponentInParent<Canvas>();
                if (root != null)
                    _uiAudio = root.GetComponentInChildren<UIAudioService>();
            }

            if (_service != null)
                RefreshState(_service.CanUndo, _service.CanRedo);
            else
                RefreshState(false, false);
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>Called by <c>Btn_Undo.onClick</c>.</summary>
        public void OnUndoPressed()
        {
            _uiAudio?.PlayClick();
            _service?.Undo();
        }

        /// <summary>Called by <c>Btn_Redo.onClick</c>.</summary>
        public void OnRedoPressed()
        {
            _uiAudio?.PlayClick();
            _service?.Redo();
        }

        #endregion

        #region Internals ?????????????????????????????????????

        private void RefreshState(bool canUndo, bool canRedo)
        {
            SetButtonState(_undoButton, _undoIcon, canUndo);
            SetButtonState(_redoButton, _redoIcon, canRedo);
        }

        private void SetButtonState(Button btn, Image icon, bool enabled)
        {
            if (btn != null)  btn.interactable = enabled;

            if (icon != null)
            {
                Color c = icon.color;
                c.a     = enabled ? _alphaEnabled : _alphaDisabled;
                icon.color = c;
            }
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void OnValidate()
        {
            if (_service    == null) Debug.LogWarning("[UndoRedoHUD] _service not assigned.",    this);
            if (_undoButton == null) Debug.LogWarning("[UndoRedoHUD] _undoButton not assigned.", this);
            if (_redoButton == null) Debug.LogWarning("[UndoRedoHUD] _redoButton not assigned.", this);
        }

        #endregion
    }
}
