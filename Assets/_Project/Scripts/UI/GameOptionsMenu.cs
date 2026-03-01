// ──────────────────────────────────────────────
//  GameOptionsMenu.cs  ·  _Project.Scripts.UI
//  Pure UI controller for the settings dropdown — delegates heavy
//  operations to WorldResetService and ScreenshotService.
// ──────────────────────────────────────────────

using System;
using UnityEngine;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// UI controller for the in-game options dropdown menu.<br/>
    /// Manages panel visibility (dropdown, blocker, confirm popup) and
    /// forwards user actions to dedicated services:<br/>
    /// • <see cref="WorldResetService"/> — block destruction, anchor reset, grid hide.<br/>
    /// • <see cref="ScreenshotService"/> — canvas-hiding screenshot capture.<br/>
    /// Attach to the <c>HUD_OptionsMenu</c> GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Game Options Menu")]
    public class GameOptionsMenu : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("UI Panels")]
        [Tooltip("The dropdown panel that appears when settings button is tapped (Panel_OptionsDropdown).")]
        [SerializeField] private GameObject _optionsPanel;

        [Tooltip("Full-screen invisible button that closes the menu when the user taps outside (HUD_MenuBlocker).")]
        [SerializeField] private GameObject _blockerPanel;

        [Tooltip("Root of the clear-all confirmation dialog (Popup_ConfirmClearAll).")]
        [SerializeField] private GameObject _confirmPopup;

        [Header("Scene References")]
        [Tooltip("Scene directional light toggled by the lighting button.")]
        [SerializeField] private Light _directionalLight;

        [Header("Services")]
        [Tooltip("Handles world reset: destroy blocks, reset anchor, deactivate grid.")]
        [SerializeField] private WorldResetService _worldResetService;

        [Tooltip("Handles screenshot capture with automatic canvas hiding.")]
        [SerializeField] private ScreenshotService _screenshotService;

        #endregion

        #region Events ────────────────────────────────────────

        /// <summary>
        /// Raised after the user confirms clear-all and the world has been
        /// fully reset. Forwards <see cref="WorldResetService.OnWorldReset"/>
        /// so UI-only listeners don't need to know about the service directly.
        /// </summary>
        public event Action OnWorldReset;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void OnEnable()
        {
            // Subscribe to service events so we can react in the UI layer.
            if (_worldResetService != null)
                _worldResetService.OnWorldReset += HandleWorldReset;

            if (_screenshotService != null)
                _screenshotService.OnScreenshotCaptured += HandleScreenshotCaptured;

            Debug.Log("[GameOptionsMenu] Subscribed to service events.");
        }

        private void OnDisable()
        {
            if (_worldResetService != null)
                _worldResetService.OnWorldReset -= HandleWorldReset;

            if (_screenshotService != null)
                _screenshotService.OnScreenshotCaptured -= HandleScreenshotCaptured;

            Debug.Log("[GameOptionsMenu] Unsubscribed from service events.");
        }

        private void Start()
        {
            // Ensure all popups start hidden.
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPopup, false);
            SetPanelActive(_blockerPanel, false);

            ValidateReferences();

            Debug.Log("[GameOptionsMenu] Initialized — all panels hidden.");
        }

        #endregion

        #region Public API (Button Callbacks) ──────────────────

        /// <summary>
        /// Toggles the options dropdown open/closed. Also shows or hides
        /// the full-screen blocker behind the menu.
        /// </summary>
        public void ToggleMenu()
        {
            if (_optionsPanel == null) return;

            bool willOpen = !_optionsPanel.activeSelf;
            _optionsPanel.SetActive(willOpen);
            SetPanelActive(_blockerPanel, willOpen);

            Debug.Log($"[GameOptionsMenu] Menu {(willOpen ? "opened" : "closed")}.");
        }

        /// <summary>
        /// Toggles the scene directional light on/off.
        /// Called by <c>Btn_Lighting</c>.
        /// </summary>
        public void ToggleLighting()
        {
            if (_directionalLight == null)
            {
                Debug.LogWarning("[GameOptionsMenu] Directional light reference is missing.");
                return;
            }

            _directionalLight.enabled = !_directionalLight.enabled;
            Debug.Log($"[GameOptionsMenu] Lighting {(_directionalLight.enabled ? "enabled" : "disabled")}.");
        }

        // ── Clear-All Flow ──────────────────────────────────

        /// <summary>
        /// Opens the confirmation popup and closes the dropdown.
        /// Called by <c>Btn_ClearAll</c>.
        /// </summary>
        public void RequestClearAll()
        {
            if (_confirmPopup == null) return;

            _confirmPopup.SetActive(true);
            ToggleMenu(); // Hide the dropdown so only the popup is visible.

            Debug.Log("[GameOptionsMenu] Clear-all requested — confirmation popup shown.");
        }

        /// <summary>
        /// Delegates to <see cref="WorldResetService.ResetWorld"/> and
        /// dismisses the confirmation popup.
        /// Called by <c>Btn_Confirm</c> inside the confirmation dialog.
        /// </summary>
        public void ConfirmClearAll()
        {
            // 1. Delegate the heavy lifting to the world-reset service.
            if (_worldResetService != null)
            {
                _worldResetService.ResetWorld();
            }
            else
            {
                Debug.LogError("[GameOptionsMenu] _worldResetService is not assigned — cannot clear world!", this);
            }

            // 2. Dismiss the popup.
            SetPanelActive(_confirmPopup, false);

            Debug.Log("[GameOptionsMenu] Clear-all confirmed — delegated to WorldResetService.");
        }

        /// <summary>
        /// Dismisses the confirmation popup without clearing anything.
        /// Called by <c>Btn_Cancel</c>.
        /// </summary>
        public void CancelClearAll()
        {
            SetPanelActive(_confirmPopup, false);
            Debug.Log("[GameOptionsMenu] Clear-all cancelled by user.");
        }

        // ── Utilities ───────────────────────────────────────

        /// <summary>
        /// Delegates screenshot capture to <see cref="ScreenshotService"/>.
        /// Called by <c>Btn_Photo</c>.
        /// </summary>
        public void TakePhoto()
        {
            if (_screenshotService != null)
            {
                _screenshotService.Capture();
                Debug.Log("[GameOptionsMenu] Photo requested — delegated to ScreenshotService.");
            }
            else
            {
                Debug.LogError("[GameOptionsMenu] _screenshotService is not assigned — cannot take photo!", this);
            }
        }

        /// <summary>
        /// Quits the application (no-op in the Editor).
        /// Called by <c>Btn_Exit</c>.
        /// </summary>
        public void ExitGame()
        {
            Debug.Log("[GameOptionsMenu] Exit requested — quitting application.");
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        #endregion

        #region Event Handlers ────────────────────────────────

        /// <summary>
        /// Forwards the world-reset event from <see cref="WorldResetService"/>
        /// to local <see cref="OnWorldReset"/> subscribers.
        /// </summary>
        private void HandleWorldReset()
        {
            OnWorldReset?.Invoke();
            Debug.Log("[GameOptionsMenu] World reset event forwarded from WorldResetService.");
        }

        /// <summary>
        /// Automatically closes the dropdown after a screenshot is captured.
        /// </summary>
        private void HandleScreenshotCaptured(string fileName)
        {
            ToggleMenu();
            Debug.Log($"[GameOptionsMenu] Screenshot event received ({fileName}) — menu closed.");
        }

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Safely sets a panel's active state with a null guard.
        /// </summary>
        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_optionsPanel == null)
                Debug.LogError("[GameOptionsMenu] _optionsPanel is not assigned!", this);
            if (_blockerPanel == null)
                Debug.LogError("[GameOptionsMenu] _blockerPanel is not assigned!", this);
            if (_confirmPopup == null)
                Debug.LogError("[GameOptionsMenu] _confirmPopup is not assigned!", this);
            if (_directionalLight == null)
                Debug.LogError("[GameOptionsMenu] _directionalLight is not assigned!", this);
            if (_worldResetService == null)
                Debug.LogError("[GameOptionsMenu] _worldResetService is not assigned!", this);
            if (_screenshotService == null)
                Debug.LogError("[GameOptionsMenu] _screenshotService is not assigned!", this);
        }

        #endregion
    }
}