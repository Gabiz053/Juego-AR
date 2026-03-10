// ──────────────────────────────────────────────
//  GameOptionsMenu.cs  ·  _Project.Scripts.UI
//  Pure UI controller for the settings dropdown — delegates heavy
//  operations to dedicated services.
// ──────────────────────────────────────────────

using System;
using UnityEngine;
using UnityEngine.UI;
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
    /// • <see cref="LightingService"/> — toggle Global/Focus lighting.<br/>
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

        [Header("Button States (toggle buttons only)")]
        [Tooltip("DropdownButtonState on Btn_Linterna — shows ON/OFF colour and label.")]
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

        [Tooltip("Manages Global/Focus lighting modes.")]
        [SerializeField] private LightingService _lightingService;

        [Tooltip("AR Plane Grid Aligner — toggles plane mesh visuals and grid lines.")]
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

            if (_depthService != null)
                _depthService.OnDepthToggled += HandleDepthToggled;

            if (_lightingService != null)
                _lightingService.OnLightingToggled += HandleLightingToggled;

            Debug.Log("[GameOptionsMenu] Subscribed to service events.");
        }

        private void OnDisable()
        {
            if (_worldResetService != null)
                _worldResetService.OnWorldReset -= HandleWorldReset;

            if (_screenshotService != null)
                _screenshotService.OnScreenshotCaptured -= HandleScreenshotCaptured;

            if (_depthService != null)
                _depthService.OnDepthToggled -= HandleDepthToggled;

            if (_lightingService != null)
                _lightingService.OnLightingToggled -= HandleLightingToggled;

            Debug.Log("[GameOptionsMenu] Unsubscribed from service events.");
        }

        private void Start()
        {
            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPopup, false);
            SetPanelActive(_blockerPanel, false);

            if (_depthService != null)
                _depthButtonState?.SetState(_depthService.IsDepthEnabled);

            if (_lightingService != null)
                _lightingButtonState?.SetState(_lightingService.IsFocusMode);

            if (_planeGridAligner != null)
            {
                // Initial state: grid lines OFF, plane visual ON.
                _planeGridAligner.SetGrid(false);
                _gridButtonState?.SetState(false);
                _planeVisualButtonState?.SetState(_planeGridAligner.IsVisualEnabled);
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
        /// Delegates to <see cref="LightingService.ToggleLighting"/>.<br/>
        /// Called by <c>Btn_Linterna</c>.
        /// </summary>
        public void ToggleLighting()
        {
            if (_lightingService == null)
            {
                Debug.LogWarning("[GameOptionsMenu] _lightingService is not assigned — operation ignored.", this);
                return;
            }

            _lightingService.ToggleLighting();
            _uiAudio?.PlayToggle();
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
        }

        /// <summary>
        /// Toggles only the grid lines on the sand shader.
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
        }

        /// <summary>
        /// Toggles the AR plane MeshRenderer on or off without stopping ARCore detection.
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
        }

        /// <summary>
        /// Sets the background music volume from the UI Slider (0–100).
        /// </summary>
        public void OnMusicVolumeChanged(float sliderValue)
        {
            if (_musicService == null) return;

            _musicService.SetVolume(sliderValue / 100f);
            RefreshMusicLabel(sliderValue);
        }

        // ── Clear-All Flow ──────────────────────────────────

        public void RequestClearAll()
        {
            if (_confirmPopup == null) return;

            _confirmPopup.SetActive(true);
            ToggleMenu();
            _uiAudio?.PlayClick();
        }

        public void ConfirmClearAll()
        {
            if (_worldResetService != null)
                _worldResetService.ResetWorld();
            else
                Debug.LogError("[GameOptionsMenu] _worldResetService is not assigned!", this);

            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayConfirm();
        }

        public void CancelClearAll()
        {
            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayCancel();
        }

        // ── Utilities ───────────────────────────────────────

        public void TakePhoto()
        {
            if (_screenshotService != null)
            {
                _screenshotService.Capture();
                _uiAudio?.PlayPhoto();
            }
            else
            {
                Debug.LogError("[GameOptionsMenu] _screenshotService is not assigned!", this);
            }
        }

        public void ExitGame()
        {
            _uiAudio?.PlayClick();
            Debug.Log("[GameOptionsMenu] Exit requested.");
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
        }

        private void HandleScreenshotCaptured(string fileName)
        {
            ToggleMenu();
        }

        private void HandleDepthToggled(bool isEnabled)
        {
            _depthButtonState?.SetState(isEnabled);
        }

        private void HandleLightingToggled(bool isFocusMode)
        {
            _lightingButtonState?.SetState(isFocusMode);
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
            if (_lightingService == null)
                Debug.LogError("[GameOptionsMenu] _lightingService is not assigned!", this);
            if (_worldResetService == null)
                Debug.LogError("[GameOptionsMenu] _worldResetService is not assigned!", this);
            if (_screenshotService == null)
                Debug.LogError("[GameOptionsMenu] _screenshotService is not assigned!", this);
            if (_depthService == null)
                Debug.LogWarning("[GameOptionsMenu] _depthService is not assigned.", this);
            if (_planeGridAligner == null)
                Debug.LogWarning("[GameOptionsMenu] _planeGridAligner is not assigned.", this);
            if (_musicService == null)
                Debug.LogWarning("[GameOptionsMenu] _musicService is not assigned.", this);
            if (_uiAudio == null)
                Debug.LogWarning("[GameOptionsMenu] _uiAudio is not assigned.", this);
        }

        #endregion
    }
}