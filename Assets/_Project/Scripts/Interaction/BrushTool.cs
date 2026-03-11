// ------------------------------------------------------------
//  BrushTool.cs  -  _Project.Scripts.Interaction
//  Continuous block-painting while the finger is held and dragged.
//  Delegates to ARBlockPlacer / BlockDestroyer / PlowTool.
// ------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using _Project.Scripts.UI;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Brush (paint) mode overlay -- hold and drag to place a continuous
    /// trail of blocks.  Independent of the active tool slot.<br/>
    /// <b>Btn_Brush.OnClick wires directly to <see cref="ToggleBrush"/></b> --
    /// the brush does NOT go through <see cref="ToolManager"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Brush Tool")]
    public class BrushTool : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ARBlockPlacer -- all placement is delegated here.")]
        [SerializeField] private ARBlockPlacer _blockPlacer;

        [Tooltip("BlockDestroyer -- continuous mining is delegated here.")]
        [SerializeField] private BlockDestroyer _blockDestroyer;

        [Tooltip("ToolManager -- listens for tool changes to track the last build tool.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("PlowTool -- continuous pebble placement when Tool_Plow is active.")]
        [SerializeField] private PlowTool _plowTool;

        [Header("Audio")]
        [Tooltip("UI audio service -- plays toggle sound.")]
        [SerializeField] private UIAudioService _uiAudio;

        [Header("Brush Settings")]
        [Tooltip("Minimum seconds between consecutive placements while dragging.")]
        [SerializeField] private float _strokeCooldown = 0.08f;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised whenever brush mode is toggled ON or OFF.</summary>
        public event Action<bool> OnBrushToggled;

        #endregion

        #region State ---------------------------------------------

        /// <summary>True while brush mode is active.</summary>
        public bool IsBrushActive { get; private set; }

        private ToolType _lastBuildTool = ToolType.Build_Sand;
        private bool     _hasBuildTool;
        private float    _lastPlaceTime   = -999f;
        private float    _lastDestroyTime = -999f;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (_toolManager != null)
                _toolManager.OnToolChanged += HandleToolChanged;
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            if (_toolManager != null)
                _toolManager.OnToolChanged -= HandleToolChanged;
        }

        private void Start()
        {
            ValidateReferences();

            if (_toolManager != null && _toolManager.IsBuildTool)
            {
                _lastBuildTool = _toolManager.CurrentTool;
                _hasBuildTool  = true;
            }

            OnBrushToggled?.Invoke(IsBrushActive);
        }

        private void Update()
        {
            if (!IsBrushActive) return;
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];

            var phase = touch.phase;
            bool isActive = phase == UnityEngine.InputSystem.TouchPhase.Began
                         || phase == UnityEngine.InputSystem.TouchPhase.Moved
                         || phase == UnityEngine.InputSystem.TouchPhase.Stationary;
            if (!isActive) return;

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId))
                return;

            if (_toolManager.CurrentTool == ToolType.Tool_Destroy)
            {
                if (Time.time - _lastDestroyTime < _strokeCooldown) return;
                _blockDestroyer?.TryDestroyBlock(touch.screenPosition);
                _lastDestroyTime = Time.time;
                return;
            }

            if (_toolManager.CurrentTool == ToolType.Tool_Plow)
            {
                _plowTool?.PlacePebbleAtScreen(touch.screenPosition);
                return;
            }

            if (Time.time - _lastPlaceTime < _strokeCooldown) return;

            EnsureBuildToolActive();
            _blockPlacer?.TryPlaceBlock(touch.screenPosition);
            _lastPlaceTime = Time.time;
        }

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Toggles brush mode ON / OFF.<br/>
        /// <b>Wire directly to Btn_Brush.OnClick in the Inspector.</b>
        /// </summary>
        public void ToggleBrush()
        {
            bool canActivate = _hasBuildTool
                            || (_toolManager != null && _toolManager.CurrentTool == ToolType.Tool_Plow)
                            || (_toolManager != null && _toolManager.CurrentTool == ToolType.Tool_Destroy);

            if (!IsBrushActive && !canActivate) return;

            IsBrushActive = !IsBrushActive;
            OnBrushToggled?.Invoke(IsBrushActive);
            _uiAudio?.PlayToggle();
            Debug.Log($"[BrushTool] Brush {(IsBrushActive ? "ON" : "OFF")}.");
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Tracks the last build tool so the brush can re-activate it.
        /// Auto-disables the brush when <see cref="ToolType.Tool_None"/> is selected.
        /// </summary>
        private void HandleToolChanged(ToolType newTool)
        {
            if (_toolManager.IsBuildTool)
            {
                _lastBuildTool = newTool;
                _hasBuildTool  = true;
            }

            if (newTool == ToolType.Tool_None)
            {
                IsBrushActive = false;
                OnBrushToggled?.Invoke(IsBrushActive);
            }
        }

        /// <summary>
        /// Restores the last-used build tool if the player currently
        /// holds a non-build tool (e.g. after switching from Destroy).
        /// </summary>
        private void EnsureBuildToolActive()
        {
            if (!_toolManager.IsBuildTool)
                _toolManager.SelectToolByIndex((int)_lastBuildTool);
        }

        private void ValidateReferences()
        {
            if (_blockPlacer == null)
                Debug.LogError("[BrushTool] _blockPlacer is not assigned!", this);
            if (_blockDestroyer == null)
                Debug.LogError("[BrushTool] _blockDestroyer is not assigned!", this);
            if (_toolManager == null)
                Debug.LogError("[BrushTool] _toolManager is not assigned!", this);
        }

        #endregion
    }
}
