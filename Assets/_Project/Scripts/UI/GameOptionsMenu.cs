// ──────────────────────────────────────────────
//  GameOptionsMenu.cs  ·  _Project.Scripts.UI
//  Pure UI controller for the settings dropdown — delegates heavy
//  operations to WorldResetService and ScreenshotService.
// ──────────────────────────────────────────────

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using _Project.Scripts.AR;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// UI controller for the in-game options dropdown menu.<br/>
    /// Manages panel visibility (dropdown, blocker, confirm popup) and
    /// forwards user actions to dedicated services:<br/>
    /// • <see cref="WorldResetService"/> — block destruction, anchor reset, grid hide.<br/>
    /// • <see cref="ScreenshotService"/> — canvas-hiding screenshot capture.<br/>
    /// • <see cref="ARDepthService"/> — toggle Depth API occlusion at runtime.<br/>
    /// • <see cref="MusicService"/> — toggle background music on/off.<br/>
    /// • <see cref="UIAudioService"/> — plays UI sound feedback on every interaction.<br/>
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

        [Header("Button States (toggle buttons only)")]
        [Tooltip("DropdownButtonState on Btn_Lighting — shows ON/OFF colour and label.")]
        [SerializeField] private DropdownButtonState _lightingButtonState;

        [Tooltip("DropdownButtonState on Btn_Depth — shows ON/OFF colour and label.")]
        [SerializeField] private DropdownButtonState _depthButtonState;

        [Tooltip("DropdownButtonState on Btn_Grid — shows ON/OFF colour and label.")]
        [SerializeField] private DropdownButtonState _gridButtonState;

        [Tooltip("DropdownButtonState on Btn_PlaneVisual — shows ON/OFF colour and label.")]
        [SerializeField] private DropdownButtonState _planeVisualButtonState;

        [Header("Services")]
        [Tooltip("Handles world reset: destroy blocks, reset anchor, deactivate grid.")]
        [SerializeField] private WorldResetService _worldResetService;

        [Tooltip("Handles screenshot capture with automatic canvas hiding.")]
        [SerializeField] private ScreenshotService _screenshotService;

        [Tooltip("Toggles ARCore Depth API occlusion on and off at runtime.")]
        [SerializeField] private ARDepthService _depthService;

        [Tooltip("AR Plane Manager — toggled by the grid button.")]
        [SerializeField] private ARPlaneManager _planeManager;

        [Tooltip("AR Plane Grid Aligner — toggles plane mesh visuals without stopping detection.")]
        [SerializeField] private ARPlaneGridAligner _planeGridAligner;

        [Tooltip("Controls background music playback volume.")]
        [SerializeField] private MusicService _musicService;

        [Tooltip("Centralised UI audio service — plays feedback sounds for every button.")]
        [SerializeField] private UIAudioService _uiAudio;

        [Header("Music Volume")]
        [Tooltip("Slider that controls music volume (Sld_MusicVolume).")]
        [SerializeField] private Slider _musicSlider;

        [Tooltip("Label above the music slider that shows the current value (Txt_MusicVolume).")]
        [SerializeField] private TMP_Text _musicVolumeLabel;

        [Tooltip("Base text shown on the music volume label before the numeric value.")]
        [SerializeField] private string _musicLabelBase = "Music";

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
            if (_worldResetService != null)
                _worldResetService.OnWorldReset += HandleWorldReset;

            if (_screenshotService != null)
                _screenshotService.OnScreenshotCaptured += HandleScreenshotCaptured;

            if (_depthService != null) _depthService.OnDepthToggled += HandleDepthToggled;

            Debug.Log("[GameOptionsMenu] Subscribed to service events.");
        }

        private void OnDisable()
        {
            if (_worldResetService != null)
                _worldResetService.OnWorldReset -= HandleWorldReset;

            if (_screenshotService != null)
                _screenshotService.OnScreenshotCaptured -= HandleScreenshotCaptured;

            if (_depthService != null) _depthService.OnDepthToggled -= HandleDepthToggled;

            Debug.Log("[GameOptionsMenu] Unsubscribed from service events.");
        }

        private void Start()
        {
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPopup, false);
            SetPanelActive(_blockerPanel, false);

            // Sync toggle button visuals to each service's initial state.
            if (_directionalLight != null)
                _lightingButtonState?.SetState(_directionalLight.enabled);

            if (_depthService != null)
                _depthButtonState?.SetState(_depthService.IsDepthEnabled);

            if (_planeGridAligner != null)
            {
                _gridButtonState?.SetState(_planeGridAligner.IsVisualEnabled);
                _planeVisualButtonState?.SetState(_planeGridAligner.IsGridEnabled);
            }

            // Sync the slider position and label to the service's initial volume.
            if (_musicService != null && _musicSlider != null)
            {
                _musicSlider.value = _musicService.Volume * 100f;
                RefreshMusicLabel(_musicSlider.value);
            }

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

            _uiAudio?.PlayMenuOpen();

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
            _lightingButtonState?.SetState(_directionalLight.enabled);
            _uiAudio?.PlayToggle();

            Debug.Log($"[GameOptionsMenu] Lighting {(_directionalLight.enabled ? "enabled" : "disabled")}.");
        }

        /// <summary>
        /// Toggles ARCore Depth API occlusion on or off.
        /// Called by <c>Btn_Depth</c>.
        /// </summary>
        public void ToggleDepth()
        {
            if (_depthService == null)
            {
                Debug.LogWarning("[GameOptionsMenu] _depthService is not assigned — operation ignored.", this);
                return;
            }

            _depthService.ToggleDepth();
            _uiAudio?.PlayToggle();

            // Visual update is handled by HandleDepthToggled via the event.
            Debug.Log($"[GameOptionsMenu] Depth toggle requested — new state: {_depthService.IsDepthEnabled}.");
        }

        /// <summary>
        /// Toggles only the grid lines on the sand shader.
        /// Sand texture stays visible — only the etched lines disappear.
        /// Called by <c>Btn_Grid</c>.
        /// </summary>
        public void ToggleGrid()
        {
            if (_planeGridAligner == null)
            {
                Debug.LogWarning("[GameOptionsMenu] _planeGridAligner is not assigned — operation ignored.", this);
                return;
            }

            bool nowVisible = !_planeGridAligner.IsGridEnabled;
            _planeGridAligner.SetGrid(nowVisible);
            _gridButtonState?.SetState(nowVisible);
            _uiAudio?.PlayToggle();

            Debug.Log($"[GameOptionsMenu] Grid lines {(nowVisible ? "shown" : "hidden")}.");
        }

        /// <summary>
        /// Toggles the AR plane MeshRenderer on or off without stopping ARCore detection.
        /// Sand and grid both disappear — ARCore keeps tracking in the background.
        /// Called by <c>Btn_PlaneVisual</c>.
        /// </summary>
        public void TogglePlaneVisual()
        {
            if (_planeGridAligner == null)
            {
                Debug.LogWarning("[GameOptionsMenu] _planeGridAligner is not assigned — operation ignored.", this);
                return;
            }

            bool nowVisible = !_planeGridAligner.IsVisualEnabled;
            _planeGridAligner.SetVisual(nowVisible);
            _planeVisualButtonState?.SetState(nowVisible);
            _uiAudio?.PlayToggle();

            Debug.Log($"[GameOptionsMenu] Plane mesh {(nowVisible ? "shown" : "hidden")}.");
        }

        /// <summary>
        /// Sets the background music volume from the UI Slider (0–1 range mapped to 0–100).
        /// Called by the <c>Sld_MusicVolume</c> Slider's <c>OnValueChanged</c> event.
        /// The Slider must be configured with Min 0, Max 100, Whole Numbers on.
        /// </summary>
        /// <param name="sliderValue">Raw slider value (0–100).</param>
        public void OnMusicVolumeChanged(float sliderValue)
        {
            if (_musicService == null)
            {
                Debug.LogWarning("[GameOptionsMenu] _musicService is not assigned — slider ignored.", this);
                return;
            }

            _musicService.SetVolume(sliderValue / 100f);
            RefreshMusicLabel(sliderValue);
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
            ToggleMenu(); // ToggleMenu already plays the menu sound.
            _uiAudio?.PlayClick();

            Debug.Log("[GameOptionsMenu] Clear-all requested — confirmation popup shown.");
        }

        /// <summary>
        /// Delegates to <see cref="WorldResetService.ResetWorld"/> and
        /// dismisses the confirmation popup.
        /// Called by <c>Btn_Confirm</c> inside the confirmation dialog.
        /// </summary>
        public void ConfirmClearAll()
        {
            if (_worldResetService != null)
                _worldResetService.ResetWorld();
            else
                Debug.LogError("[GameOptionsMenu] _worldResetService is not assigned — cannot clear world!", this);

            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayConfirm();

            Debug.Log("[GameOptionsMenu] Clear-all confirmed — delegated to WorldResetService.");
        }

        /// <summary>
        /// Dismisses the confirmation popup without clearing anything.
        /// Called by <c>Btn_Cancel</c>.
        /// </summary>
        public void CancelClearAll()
        {
            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayCancel();

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
                _uiAudio?.PlayPhoto();

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
            _uiAudio?.PlayClick();

            Debug.Log("[GameOptionsMenu] Exit requested — quitting application.");
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        #endregion

        #region Event Handlers ────────────────────────────────

        private void HandleWorldReset()
        {
            OnWorldReset?.Invoke();
            Debug.Log("[GameOptionsMenu] World reset event forwarded from WorldResetService.");
        }

        private void HandleScreenshotCaptured(string fileName)
        {
            ToggleMenu();
            Debug.Log($"[GameOptionsMenu] Screenshot event received ({fileName}) — menu closed.");
        }

        private void HandleDepthToggled(bool isEnabled)
        {
            _depthButtonState?.SetState(isEnabled);
        }

        #endregion

        #region Internals ─────────────────────────────────────

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private void RefreshMusicLabel(float sliderValue)
        {
            if (_musicVolumeLabel != null)
                _musicVolumeLabel.text = $"{_musicLabelBase}  {Mathf.RoundToInt(sliderValue)}";
        }

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
            if (_depthService == null)
                Debug.LogWarning("[GameOptionsMenu] _depthService is not assigned — Btn_Depth will not work.", this);
            if (_planeManager == null)
                Debug.LogWarning("[GameOptionsMenu] _planeManager is not assigned — Btn_Grid will not work.", this);
            if (_planeGridAligner == null)
                Debug.LogWarning("[GameOptionsMenu] _planeGridAligner is not assigned — Btn_PlaneVisual will not work.", this);
            if (_musicService == null)
                Debug.LogWarning("[GameOptionsMenu] _musicService is not assigned — music slider will not work.", this);
            if (_musicSlider == null)
                Debug.LogWarning("[GameOptionsMenu] _musicSlider is not assigned — slider will not sync.", this);
            if (_uiAudio == null)
                Debug.LogWarning("[GameOptionsMenu] _uiAudio is not assigned — UI will have no sound.", this);
        }

        #endregion
    }
}