// ------------------------------------------------------------
//  GameOptionsMenu.cs  -  _Project.Scripts.UI
//  Pure UI controller for the settings dropdown -- delegates
//  heavy operations to dedicated services.
// ------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project.Scripts.AR;
using _Project.Scripts.Core;

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
        #region Inspector -----------------------------------------

        [Header("UI Panels")]
        [SerializeField] private GameObject _optionsPanel;
        [SerializeField] private GameObject _blockerPanel;
        [SerializeField] private GameObject _confirmPopup;

        [Header("Button States (toggles)")]
        [SerializeField] private DropdownButtonState _lightingButtonState;
        [SerializeField] private DropdownButtonState _depthButtonState;
        [SerializeField] private DropdownButtonState _gridButtonState;
        [SerializeField] private DropdownButtonState _planeVisualButtonState;

        [Header("Services")]
        [SerializeField] private WorldResetService  _worldResetService;
        [SerializeField] private ScreenshotService  _screenshotService;
        [SerializeField] private ARDepthService     _depthService;
        [SerializeField] private LightingService    _lightingService;
        [SerializeField] private ARPlaneGridAligner _planeGridAligner;
        [SerializeField] private MusicService       _musicService;
        [SerializeField] private UIAudioService     _uiAudio;

        [Header("Music Volume")]
        [SerializeField] private Slider   _musicSlider;
        [SerializeField] private TMP_Text _musicVolumeLabel;
        [SerializeField] private string   _musicLabelBase = "Music";

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised after the user confirms clear-all.</summary>
        public event Action OnWorldReset;

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
                _planeGridAligner.SetGrid(false);
                _gridButtonState?.SetState(false);
                _planeVisualButtonState?.SetState(_planeGridAligner.IsVisualEnabled);
            }

            if (_musicService != null && _musicSlider != null)
            {
                _musicSlider.value = _musicService.Volume * 100f;
                RefreshMusicLabel(_musicSlider.value);
            }

            ValidateReferences();
        }

        #endregion

        #region Public API ----------------------------------------

        public void ToggleMenu()
        {
            if (_optionsPanel == null) return;

            bool willOpen = !_optionsPanel.activeSelf;
            _optionsPanel.SetActive(willOpen);
            SetPanelActive(_blockerPanel, willOpen);
            _uiAudio?.PlayMenuOpen();
        }

        public void ToggleLighting()
        {
            if (_lightingService == null) return;
            _lightingService.ToggleLighting();
            _uiAudio?.PlayToggle();
        }

        public void ToggleDepth()
        {
            if (_depthService == null) return;
            _depthService.ToggleDepth();
            _uiAudio?.PlayToggle();
        }

        public void ToggleGrid()
        {
            if (_planeGridAligner == null) return;
            bool nowVisible = !_planeGridAligner.IsGridEnabled;
            _planeGridAligner.SetGrid(nowVisible);
            _gridButtonState?.SetState(nowVisible);
            _uiAudio?.PlayToggle();
        }

        public void TogglePlaneVisual()
        {
            if (_planeGridAligner == null) return;
            bool nowVisible = !_planeGridAligner.IsVisualEnabled;
            _planeGridAligner.SetVisual(nowVisible);
            _planeVisualButtonState?.SetState(nowVisible);
            _uiAudio?.PlayToggle();
        }

        public void OnMusicVolumeChanged(float sliderValue)
        {
            if (_musicService == null) return;
            _musicService.SetVolume(sliderValue / 100f);
            RefreshMusicLabel(sliderValue);
        }

        // -- Clear-All Flow --------------------------------------

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
            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayConfirm();
        }

        public void CancelClearAll()
        {
            SetPanelActive(_confirmPopup, false);
            _uiAudio?.PlayCancel();
        }

        // -- Utilities -------------------------------------------

        public void TakePhoto()
        {
            if (_screenshotService == null) return;
            _screenshotService.Capture();
            _uiAudio?.PlayPhoto();
        }

        public void ExitGame()
        {
            _uiAudio?.PlayClick();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        #endregion

        #region Event Handlers ------------------------------------

        private void HandleWorldReset()         => OnWorldReset?.Invoke();
        private void HandleScreenshotCaptured(string _) => ToggleMenu();
        private void HandleDepthToggled(bool on) => _depthButtonState?.SetState(on);
        private void HandleLightingToggled(bool on) => _lightingButtonState?.SetState(on);

        #endregion

        #region Internals -----------------------------------------

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
            if (_worldResetService == null)
                Debug.LogError("[GameOptionsMenu] _worldResetService is not assigned!", this);
            if (_screenshotService == null)
                Debug.LogError("[GameOptionsMenu] _screenshotService is not assigned!", this);
        }

        #endregion
    }
}