// ------------------------------------------------------------
//  GameOptionsMenu.cs  -  _Project.Scripts.UI
//  UI controller for the in-game settings dropdown.
// ------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using TMPro;
using _Project.Scripts.AR;
using _Project.Scripts.Core;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// UI controller for the in-game options dropdown.  Manages panel
    /// visibility and forwards user actions to dedicated services.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/Game Options Menu")]
    public class GameOptionsMenu : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Scene name loaded when returning to the title screen.</summary>
        private const string TITLE_SCENE = "Title_Screen";

        /// <summary>Maximum value of the music volume slider (maps to 0-1).</summary>
        private const float MUSIC_SLIDER_MAX = 100f;

        #endregion

        #region Inspector -----------------------------------------

        [Header("UI Panels")]
        [Tooltip("The main options dropdown panel.")]
        [SerializeField] private GameObject _optionsPanel;
        [Tooltip("Full-screen invisible blocker behind the dropdown.")]
        [SerializeField] private GameObject _blockerPanel;
        [Tooltip("Confirmation popup for clear-all action.")]
        [SerializeField] private GameObject _confirmPopup;

        [Header("Button States (toggles)")]
        [Tooltip("Visual state controller for the lighting toggle.")]
        [SerializeField] private DropdownButtonState _lightingButtonState;
        [Tooltip("Visual state controller for the depth toggle.")]
        [SerializeField] private DropdownButtonState _depthButtonState;
        [Tooltip("Visual state controller for the grid toggle.")]
        [SerializeField] private DropdownButtonState _gridButtonState;
        [Tooltip("Visual state controller for the plane visual toggle.")]
        [SerializeField] private DropdownButtonState _planeVisualButtonState;
        [Tooltip("Visual state controller for the vibration toggle.")]
        [SerializeField] private DropdownButtonState _vibrationButtonState;

        [Header("Services")]
        [Tooltip("Service that handles world clear-all and reset.")]
        [SerializeField] private WorldResetService  _worldResetService;
        [Tooltip("Service that captures and saves screenshots.")]
        [SerializeField] private ScreenshotService  _screenshotService;
        [Tooltip("Service that toggles AR depth occlusion.")]
        [SerializeField] private ARDepthService     _depthService;
        [Tooltip("Service that toggles focus/global lighting.")]
        [SerializeField] private LightingService    _lightingService;
        [Tooltip("Service that manages haptic vibration.")]
        [SerializeField] private HapticService      _hapticService;
        [Tooltip("Aligner that manages AR plane grid and visual.")]
        [SerializeField] private ARPlaneGridAligner _planeGridAligner;
        [Tooltip("Service that controls background music playback.")]
        [SerializeField] private MusicService       _musicService;

        [Header("Save")]
        [Tooltip("Popup for naming and saving the current garden.")]
        [SerializeField] private SaveGardenPopup _saveGardenPopup;

        [Header("Music Volume")]
        [Tooltip("Slider that controls music volume (0-100).")]
        [SerializeField] private Slider   _musicSlider;
        [Tooltip("Label showing current music volume percentage.")]
        [SerializeField] private TMP_Text _musicVolumeLabel;
        [Tooltip("Base text prepended to the volume number.")]
        [SerializeField] private string   _musicLabelBase = "Music";

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised after the user confirms clear-all.</summary>
        public event Action OnWorldReset;

        #endregion

        #region State ---------------------------------------------

        private IUIAudioService _uiAudio;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Opens or closes the options dropdown.</summary>
        public void ToggleMenu()
        {
            if (_optionsPanel == null) return;

            bool willOpen = !_optionsPanel.activeSelf;
            _optionsPanel.SetActive(willOpen);
            SetPanelActive(_blockerPanel, willOpen);
            _uiAudio?.PlayMenuOpen();
            Debug.Log($"[GameOptionsMenu] Menu {(willOpen ? "opened" : "closed")}.");
        }

        /// <summary>Toggles between Global and Focus lighting modes.</summary>
        public void ToggleLighting()
        {
            if (_lightingService == null) return;
            _lightingService.ToggleLighting();
            _uiAudio?.PlayToggle();
        }

        /// <summary>Toggles AR depth occlusion ON / OFF.</summary>
        public void ToggleDepth()
        {
            Debug.Log("[GameOptionsMenu] ToggleDepth pressed.");

            if (_depthService == null)
            {
                Debug.LogWarning("[GameOptionsMenu] _depthService is null -- cannot toggle depth.");
                return;
            }

            _depthService.ToggleDepth();
            _uiAudio?.PlayToggle();
        }

        /// <summary>Toggles the grid-line overlay on detected AR planes.</summary>
        public void ToggleGrid()
        {
            if (_planeGridAligner == null) return;
            bool nowVisible = !_planeGridAligner.IsGridEnabled;
            _planeGridAligner.SetGrid(nowVisible);
            _gridButtonState?.SetState(nowVisible);
            _uiAudio?.PlayToggle();
        }

        /// <summary>Toggles the AR plane mesh visual ON / OFF.</summary>
        public void TogglePlaneVisual()
        {
            if (_planeGridAligner == null) return;
            bool nowVisible = !_planeGridAligner.IsVisualEnabled;
            _planeGridAligner.SetVisual(nowVisible);
            _planeVisualButtonState?.SetState(nowVisible);
            _uiAudio?.PlayToggle();
        }

        /// <summary>Toggles haptic vibration ON / OFF.</summary>
        public void ToggleVibration()
        {
            if (_hapticService == null) return;
            _hapticService.ToggleHaptics();
            _uiAudio?.PlayToggle();
        }

        /// <summary>Called by the music slider -- maps 0-100 to 0-1 volume.</summary>
        public void OnMusicVolumeChanged(float sliderValue)
        {
            if (_musicService == null) return;
            _musicService.SetVolume(sliderValue / MUSIC_SLIDER_MAX);
            RefreshMusicLabel(sliderValue);
        }

        /// <summary>Shows the confirmation popup and closes the options panel.</summary>
        public void RequestClearAll()
        {
            if (_confirmPopup == null) return;
            _confirmPopup.SetActive(true);
            ToggleMenu();
            _uiAudio?.PlayClick();
        }

        /// <summary>Executes the full world reset after user confirmation.</summary>
        public void ConfirmClearAll()
        {
            if (_worldResetService != null)
                _worldResetService.ResetWorld();
            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayConfirm();
            Debug.Log("[GameOptionsMenu] Clear-all confirmed.");
        }

        /// <summary>Dismisses the confirmation popup without clearing.</summary>
        public void CancelClearAll()
        {
            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayCancel();
        }

        /// <summary>Triggers a screenshot capture.</summary>
        public void TakePhoto()
        {
            if (_screenshotService == null) return;
            _screenshotService.Capture();
        }

        /// <summary>Opens the save-garden popup and closes the dropdown.</summary>
        public void SaveGarden()
        {
            if (_saveGardenPopup == null) return;
            _saveGardenPopup.Show();
            ToggleMenu();
            _uiAudio?.PlayClick();
            Debug.Log("[GameOptionsMenu] Save garden popup opened.");
        }

        /// <summary>Returns to the title screen scene with a fade transition.</summary>
        public void ExitGame()
        {
            _uiAudio?.PlayClick();
            Debug.Log($"[GameOptionsMenu] Returning to title screen -- transitioning to {TITLE_SCENE}.");
            StartCoroutine(ExitWithARCleanup());
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

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
            if (_hapticService != null)
                _hapticService.OnHapticsToggled += HandleHapticsToggled;
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
            if (_hapticService != null)
                _hapticService.OnHapticsToggled -= HandleHapticsToggled;
        }

        private void Start()
        {
            ServiceLocator.TryGet<IUIAudioService>(out _uiAudio);

            SetPanelActive(_optionsPanel, false);
            SetPanelActive(_confirmPopup, false);
            SetPanelActive(_blockerPanel, false);

            if (_depthService != null)
                _depthButtonState?.SetState(_depthService.IsDepthEnabled);
            if (_lightingService != null)
                _lightingButtonState?.SetState(_lightingService.IsFocusMode);

            if (_planeGridAligner != null)
            {
                _planeGridAligner.SetGrid(false);
                _gridButtonState?.SetState(false);
                _planeVisualButtonState?.SetState(_planeGridAligner.IsVisualEnabled);
            }

            if (_hapticService != null)
                _vibrationButtonState?.SetState(_hapticService.IsEnabled);

            if (_musicService != null && _musicSlider != null)
            {
                _musicSlider.value = _musicService.Volume * MUSIC_SLIDER_MAX;
                RefreshMusicLabel(_musicSlider.value);
            }

            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Fully deinitializes the XR loader to destroy the native ARCore
        /// session, then re-initializes a clean loader before transitioning
        /// to the title screen.  Previous approaches (disabling AR managers,
        /// disabling <see cref="ARSession"/>, waiting frames) all failed
        /// because ARCore's native session is a process-level singleton that
        /// retains its <c>PlanarTargetTrackingManager</c> image-tracking
        /// configuration across pause/resume cycles.  Only
        /// <see cref="XRManagerSettings.DeinitializeLoader"/> destroys the
        /// native session and clears that stale config, preventing the
        /// SIGSEGV in <c>ArSession_resume</c>.
        /// </summary>
        private IEnumerator ExitWithARCleanup()
        {
            // 1. Disable AR feature managers so they cleanly remove their
            //    features from the native session configuration.
            ARTrackedImageManager imageManager = FindAnyObjectByType<ARTrackedImageManager>();
            if (imageManager != null)
            {
                imageManager.enabled = false;
                Debug.Log("[GameOptionsMenu] ARTrackedImageManager disabled.");
            }

            ARPlaneManager planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (planeManager != null)
            {
                planeManager.enabled = false;
                Debug.Log("[GameOptionsMenu] ARPlaneManager disabled.");
            }

            // 2. Wait one frame so ARCore processes the feature removal.
            yield return null;

            // 3. Fully deinitialize the XR loader -- this destroys the
            //    native ARCore session and all its retained configuration.
            XRGeneralSettings xrSettings = XRGeneralSettings.Instance;
            XRManagerSettings xrManager  = xrSettings != null ? xrSettings.Manager : null;

            if (xrManager != null && xrManager.isInitializationComplete)
            {
                xrManager.DeinitializeLoader();
                Debug.Log("[GameOptionsMenu] XR loader deinitialized -- native ARCore session destroyed.");
            }

            // 4. Re-initialize a fresh loader so the next scene's ARSession
            //    finds a working XR environment (with no stale features).
            if (xrManager != null)
            {
                xrManager.InitializeLoaderSync();
                Debug.Log("[GameOptionsMenu] XR loader re-initialized (clean session).");
            }

            // 5. Proceed with the fade-to-black + scene load.
            SceneTransitionService.EnsureAvailable();

            if (ServiceLocator.TryGet<ISceneTransitionService>(out var transition))
                transition.TransitionTo(TITLE_SCENE);
        }

        /// <summary>Relays <see cref="WorldResetService.OnWorldReset"/> to local subscribers.</summary>
        private void HandleWorldReset()         => OnWorldReset?.Invoke();

        /// <summary>
        /// Closes the menu after a short delay so the
        /// <see cref="ButtonPressAnimation"/> squeeze can finish before
        /// the dropdown is deactivated.
        /// </summary>
        private void HandleScreenshotCaptured(string _) => StartCoroutine(CloseMenuDelayed());

        /// <summary>Syncs the depth button dim state with the service.</summary>
        private void HandleDepthToggled(bool on) => _depthButtonState?.SetState(on);

        /// <summary>Syncs the lighting button dim state with the service.</summary>
        private void HandleLightingToggled(bool on) => _lightingButtonState?.SetState(on);

        /// <summary>Syncs the vibration button dim state with the service.</summary>
        private void HandleHapticsToggled(bool on) => _vibrationButtonState?.SetState(on);

        /// <summary>
        /// Waits one frame so button animations settle, then closes the
        /// options dropdown.
        /// </summary>
        private IEnumerator CloseMenuDelayed()
        {
            yield return null;
            ToggleMenu();
        }

        /// <summary>Null-safe shortcut for <c>GameObject.SetActive</c>.</summary>
        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        /// <summary>Updates the music volume label text beside the slider.</summary>
        private void RefreshMusicLabel(float sliderValue)
        {
            if (_musicVolumeLabel != null)
                _musicVolumeLabel.text = $"{_musicLabelBase}  {Mathf.RoundToInt(sliderValue)}";
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_optionsPanel == null)
                Debug.LogWarning("[GameOptionsMenu] _optionsPanel is not assigned.", this);
            if (_worldResetService == null)
                Debug.LogWarning("[GameOptionsMenu] _worldResetService is not assigned.", this);
            if (_screenshotService == null)
                Debug.LogWarning("[GameOptionsMenu] _screenshotService is not assigned.", this);
            if (_depthService == null)
                Debug.LogWarning("[GameOptionsMenu] _depthService is not assigned.", this);
            if (_lightingService == null)
                Debug.LogWarning("[GameOptionsMenu] _lightingService is not assigned.", this);
        }

        #endregion
    }
}