// ──────────────────────────────────────────────
//  UIManager.cs  ·  _Project.Scripts.UI
//  Manages the HUD selector highlight and toolbar slot clicks.
// ──────────────────────────────────────────────

using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Controls the visual selector highlight that follows the currently
    /// selected tool/block slot, and forwards slot clicks to
    /// <see cref="ToolManager"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/UI Manager")]
    public class UIManager : MonoBehaviour
    {
        #region Constants ─────────────────────────────────────

        /// <summary>
        /// Short delay (seconds) before the first selector positioning.
        /// Allows Layout Groups to finish arranging buttons.
        /// </summary>
        private const float LAYOUT_SETTLE_DELAY = 0.1f;

        #endregion

        #region Inspector ─────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("Reference to the ToolManager that owns the current tool state.")]
        [SerializeField] private ToolManager _toolManager;

        [Header("Selector Visuals")]
        [Tooltip("RectTransform of the yellow highlight that moves between slots.")]
        [SerializeField] private RectTransform _selectorRect;

        [Tooltip("Ordered array of every slot RectTransform (index must match ToolType int value).")]
        [SerializeField] private RectTransform[] _slotRects;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void OnEnable()
        {
            if (_toolManager != null)
            {
                _toolManager.OnToolChanged += HandleToolChanged;
                Debug.Log("[UIManager] Subscribed to ToolManager.OnToolChanged.");
            }
        }

        private void OnDisable()
        {
            if (_toolManager != null)
            {
                _toolManager.OnToolChanged -= HandleToolChanged;
                Debug.Log("[UIManager] Unsubscribed from ToolManager.OnToolChanged.");
            }
        }

        private void Start()
        {
            ValidateReferences();

            // Wait a short delay so Layout Groups finish arranging buttons
            // before we position the selector for the first time.
            Invoke(nameof(ForceInitialSelection), LAYOUT_SETTLE_DELAY);

            Debug.Log("[UIManager] Initialized. Waiting for layout settle before first selection.");
        }

        #endregion

        #region Public API (Button Callbacks) ──────────────────

        /// <summary>
        /// Called by UI Button <c>OnClick</c> events. Forwards the slot
        /// index to <see cref="ToolManager.SelectToolByIndex"/>.
        /// </summary>
        /// <param name="index">Slot index matching the <c>ToolType</c> int value.</param>
        public void OnSlotClicked(int index)
        {
            Debug.Log($"[UIManager] Slot clicked — index {index}.");
            _toolManager.SelectToolByIndex(index);
        }

        /// <summary>
        /// Forces the selector to refresh its position based on the current tool.
        /// Useful after layout rebuilds or orientation changes.
        /// </summary>
        public void RefreshSelector()
        {
            if (_toolManager != null)
            {
                HandleToolChanged(_toolManager.CurrentTool);
            }
        }

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Positions and resizes the selector highlight over the active slot.
        /// </summary>
        private void HandleToolChanged(ToolType newTool)
        {
            int toolIndex = (int)newTool;

            if (toolIndex < 0 || toolIndex >= _slotRects.Length)
            {
                Debug.LogWarning($"[UIManager] Tool index {toolIndex} ({newTool}) is out of range " +
                                 $"(slots: {_slotRects.Length}). Selector not moved.");
                return;
            }

            RectTransform targetSlot = _slotRects[toolIndex];

            // Move the selector to the exact screen position of the target slot.
            _selectorRect.position = targetSlot.position;

            // Match the slot's width and height so the highlight fits perfectly.
            _selectorRect.sizeDelta = new Vector2(targetSlot.rect.width, targetSlot.rect.height);

            Debug.Log($"[UIManager] Selector moved to {newTool} (index {toolIndex}).");
        }

        /// <summary>
        /// Called once after a short delay so Layout Groups have settled.
        /// </summary>
        private void ForceInitialSelection()
        {
            if (_toolManager != null)
            {
                HandleToolChanged(_toolManager.CurrentTool);
                Debug.Log($"[UIManager] Initial selection applied: {_toolManager.CurrentTool}.");
            }
        }

        /// <summary>
        /// Logs warnings for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[UIManager] _toolManager is not assigned!", this);
            if (_selectorRect == null)
                Debug.LogError("[UIManager] _selectorRect is not assigned!", this);
            if (_slotRects == null || _slotRects.Length == 0)
                Debug.LogError("[UIManager] _slotRects array is empty or null!", this);
        }

        #endregion
    }
}