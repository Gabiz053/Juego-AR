// ──────────────────────────────────────────────
//  OrientationManager.cs  ·  _Project.Scripts.UI
//  Hides build UI in landscape and restores it in portrait.
// ──────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Monitors device orientation every frame and hides the block hotbar,
    /// tool panel, and selector when the device is in landscape mode.
    /// On return to portrait the previous tool selection is restored.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Orientation Manager")]
    public class OrientationManager : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("UI Elements to Hide in Landscape")]
        [Tooltip("The block hotbar panel (HUD_Hotbar).")]
        [SerializeField] private GameObject _hotbarPanel;

        [Tooltip("The tool panel (HUD_ToolPanel).")]
        [SerializeField] private GameObject _toolPanel;

        [Tooltip("The yellow selector highlight (HUD_Selector).")]
        [SerializeField] private GameObject _selectorHighlight;

        [Header("Dependencies")]
        [Tooltip("ToolManager — used to force Tool_None in landscape and restore on portrait.")]
        [SerializeField] private ToolManager _toolManager;

        #endregion

        #region Private State ─────────────────────────────────

        /// <summary>Cached landscape state to detect changes.</summary>
        private bool _isLandscape;

        /// <summary>Tool the user had before entering landscape.</summary>
        private ToolType _previousTool = ToolType.Build_Dirt;

        /// <summary>Cached yield instruction — avoids GC allocation per orientation change.</summary>
        private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Start()
        {
            ValidateReferences();
            EvaluateOrientation();

            Debug.Log($"[OrientationManager] Initialized — landscape: {_isLandscape}.");
        }

        private void Update()
        {
            EvaluateOrientation();
        }

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Checks current screen dimensions and fires
        /// <see cref="OnOrientationChanged"/> once when orientation flips.
        /// </summary>
        private void EvaluateOrientation()
        {
            bool currentIsLandscape = Screen.width > Screen.height;

            if (currentIsLandscape != _isLandscape)
            {
                _isLandscape = currentIsLandscape;
                OnOrientationChanged(_isLandscape);
            }
        }

        /// <summary>
        /// Called once per orientation transition. Shows or hides the
        /// build UI and manages the tool selection accordingly.
        /// </summary>
        private void OnOrientationChanged(bool landscape)
        {
            bool showBuildUI = !landscape;

            // Show or hide the hotbar, tool panel, and selector.
            SetActive(_hotbarPanel, showBuildUI);
            SetActive(_toolPanel, showBuildUI);

            if (landscape)
            {
                // Save the current tool so we can restore it later.
                if (_toolManager != null)
                    _previousTool = _toolManager.CurrentTool;

                // Hide the selector and force an empty hand to prevent
                // accidental block placement in landscape.
                SetActive(_selectorHighlight, false);

                if (_toolManager != null)
                    _toolManager.SelectToolByIndex((int)ToolType.Tool_None);

                Debug.Log($"[OrientationManager] Switched to LANDSCAPE — saved tool: {_previousTool}, forced Tool_None.");
            }
            else
            {
                // Re-show the selector and wait one frame for Layout Groups
                // to settle before restoring the previous tool.
                SetActive(_selectorHighlight, true);
                StartCoroutine(RestoreToolAfterLayout());

                Debug.Log($"[OrientationManager] Switched to PORTRAIT — restoring tool: {_previousTool}.");
            }
        }

        /// <summary>
        /// Waits one frame so Layout Groups recalculate, then restores
        /// the previously selected tool.
        /// </summary>
        private IEnumerator RestoreToolAfterLayout()
        {
            yield return _waitEndOfFrame;

            if (_toolManager != null)
            {
                _toolManager.SelectToolByIndex((int)_previousTool);
                Debug.Log($"[OrientationManager] Tool restored to {_previousTool} after layout rebuild.");
            }
        }

        /// <summary>
        /// Safely sets a GameObject's active state with a null guard.
        /// </summary>
        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        /// <summary>
        /// Logs warnings for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_hotbarPanel == null)
                Debug.LogError("[OrientationManager] _hotbarPanel is not assigned!", this);
            if (_toolPanel == null)
                Debug.LogError("[OrientationManager] _toolPanel is not assigned!", this);
            if (_selectorHighlight == null)
                Debug.LogError("[OrientationManager] _selectorHighlight is not assigned!", this);
            if (_toolManager == null)
                Debug.LogError("[OrientationManager] _toolManager is not assigned!", this);
        }

        #endregion
    }
}