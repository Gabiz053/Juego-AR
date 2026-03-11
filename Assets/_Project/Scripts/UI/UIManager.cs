// ------------------------------------------------------------
//  UIManager.cs  -  _Project.Scripts.UI
//  Manages the HUD selector highlight and toolbar slot clicks.
// ------------------------------------------------------------

using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Controls the visual selector highlight and forwards slot
    /// clicks to <see cref="ToolManager"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/UI Manager")]
    public class UIManager : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const float LAYOUT_SETTLE_DELAY = 0.1f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ToolManager that owns the current tool state.")]
        [SerializeField] private ToolManager _toolManager;

        [Header("Selector Visuals")]
        [Tooltip("RectTransform of the highlight that moves between slots.")]
        [SerializeField] private RectTransform _selectorRect;

        [Tooltip("Ordered slot RectTransforms (index = ToolType int value).")]
        [SerializeField] private RectTransform[] _slotRects;

        [Header("Audio")]
        [Tooltip("UI audio service -- plays sound on slot selection.")]
        [SerializeField] private UIAudioService _uiAudio;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void OnEnable()
        {
            if (_toolManager != null)
                _toolManager.OnToolChanged += HandleToolChanged;
        }

        private void OnDisable()
        {
            if (_toolManager != null)
                _toolManager.OnToolChanged -= HandleToolChanged;
        }

        private void Start()
        {
            ValidateReferences();
            Invoke(nameof(ForceInitialSelection), LAYOUT_SETTLE_DELAY);
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Called by UI Button OnClick events.</summary>
        public void OnSlotClicked(int index)
        {
            _toolManager.SelectToolByIndex(index);
            _uiAudio?.PlaySlotSelect();
        }

        /// <summary>Forces the selector to refresh after layout changes.</summary>
        public void RefreshSelector()
        {
            if (_toolManager != null)
                HandleToolChanged(_toolManager.CurrentTool);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Moves the selector highlight to the slot matching <paramref name="newTool"/>.
        /// </summary>
        private void HandleToolChanged(ToolType newTool)
        {
            int toolIndex = (int)newTool;
            if (toolIndex < 0 || toolIndex >= _slotRects.Length) return;

            RectTransform targetSlot = _slotRects[toolIndex];
            _selectorRect.position  = targetSlot.position;
            _selectorRect.sizeDelta = new Vector2(targetSlot.rect.width, targetSlot.rect.height);
        }

        /// <summary>Deferred call so the layout system settles before positioning the selector.</summary>
        private void ForceInitialSelection()
        {
            if (_toolManager != null)
                HandleToolChanged(_toolManager.CurrentTool);
        }

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[UIManager] _toolManager is not assigned!", this);
            if (_selectorRect == null)
                Debug.LogError("[UIManager] _selectorRect is not assigned!", this);
            if (_slotRects == null || _slotRects.Length == 0)
                Debug.LogError("[UIManager] _slotRects array is empty!", this);
        }

        #endregion
    }
}