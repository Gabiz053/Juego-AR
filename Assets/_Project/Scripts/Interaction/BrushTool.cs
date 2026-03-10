// ??????????????????????????????????????????????
//  BrushTool.cs  ·  _Project.Scripts.Interaction
//  Continuous block-painting while the finger is held and dragged.
//  Hooks into ARBlockPlacer for placement and BlockDestroyer for
//  mining — zero duplication.
// ??????????????????????????????????????????????

using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using _Project.Scripts.UI;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Brush (paint) tool — hold and drag to place a continuous trail of blocks.<br/>
    /// The brush is a <b>mode toggle</b> independent of the active tool slot:<br/>
    /// • Changing block type keeps the brush ON.<br/>
    /// • Selecting the Destroy tool keeps the brush ON (destroy + brush coexist).<br/>
    /// Fires <see cref="OnBrushToggled"/> so <see cref="BrushHUD"/> can
    /// update the button visual.<br/>
    /// <b>Btn_Brush.OnClick wires directly to <see cref="ToggleBrush"/></b> —
    /// the brush does NOT go through <see cref="ToolManager"/> because it is
    /// a mode overlay, not a regular tool.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Brush Tool")]
    public class BrushTool : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("ARBlockPlacer — all placement is delegated here.")]
        [SerializeField] private ARBlockPlacer _blockPlacer;

        [Tooltip("BlockDestroyer — continuous mining is delegated here.")]
        [SerializeField] private BlockDestroyer _blockDestroyer;

        [Tooltip("ToolManager — listens for tool changes to track the last build tool.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("PlowTool — receives continuous placement calls when Tool_Plow is active.")]
        [SerializeField] private PlowTool _plowTool;

        [Header("Audio")]
        [Tooltip("UI audio service — plays toggle sound when brush is turned on or off.")]
        [SerializeField] private UIAudioService _uiAudio;

        [Header("Brush Settings")]
        [Tooltip("Minimum seconds between consecutive block placements while dragging.\n" +
                 "0.08 s ? one block per 5 frames at 60 fps. Raise for sparser trails.")]
        [SerializeField] private float _strokeCooldown = 0.08f;

        #endregion

        #region Events ????????????????????????????????????????

        /// <summary>
        /// Raised whenever brush mode is toggled ON or OFF.<br/>
        /// The <see langword="bool"/> parameter is <c>true</c> when brush is now active.
        /// Listened by <see cref="BrushHUD"/> on <c>Btn_Brush</c>.
        /// </summary>
        public event Action<bool> OnBrushToggled;

        #endregion

        #region Runtime State ?????????????????????????????????

        /// <summary>True while brush mode is active (toggled by <see cref="ToggleBrush"/>).</summary>
        public bool IsBrushActive { get; private set; }

        /// <summary>The last build tool that was active before or during brush mode.</summary>
        private ToolType _lastBuildTool = ToolType.Build_Sand;

        /// <summary>
        /// True once the player has explicitly selected a build tool at least once.
        /// Prevents the brush from activating on Tool_None using the default seed value.
        /// </summary>
        private bool _hasBuildTool;

        private float _lastPlaceTime   = -999f;
        private float _lastDestroyTime = -999f;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

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

            // Seed with whatever build tool is already selected.
            if (_toolManager != null && _toolManager.IsBuildTool)
            {
                _lastBuildTool = _toolManager.CurrentTool;
                _hasBuildTool  = true;
            }

            // Notify listeners of initial state (OFF).
            OnBrushToggled?.Invoke(IsBrushActive);
        }

        private void Update()
        {
            if (!IsBrushActive) return;
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];

            // Accept Began, Moved AND Stationary so held finger keeps firing.
            var phase = touch.phase;
            bool isActive = phase == UnityEngine.InputSystem.TouchPhase.Began
                         || phase == UnityEngine.InputSystem.TouchPhase.Moved
                         || phase == UnityEngine.InputSystem.TouchPhase.Stationary;
            if (!isActive) return;

            // Ignore touches that land on UI.
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId))
                return;

            // Destroy mode: mine continuously while finger is held.
            if (_toolManager.CurrentTool == ToolType.Tool_Destroy)
            {
                if (Time.time - _lastDestroyTime < _strokeCooldown) return;
                _blockDestroyer?.TryDestroyBlock(touch.screenPosition);
                _lastDestroyTime = Time.time;
                return;
            }

            // Plow mode: scatter pebbles continuously (no grid alignment).
            if (_toolManager.CurrentTool == ToolType.Tool_Plow)
            {
                _plowTool?.PlacePebbleAtScreen(touch.screenPosition);
                return;
            }

            // Build mode: paint blocks continuously.
            if (Time.time - _lastPlaceTime < _strokeCooldown) return;

            EnsureBuildToolActive();
            _blockPlacer?.TryPlaceBlock(touch.screenPosition);
            _lastPlaceTime = Time.time;
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Toggles brush mode ON or OFF.<br/>
        /// <b>Wire this directly to <c>Btn_Brush</c> OnClick in the Inspector.</b><br/>
        /// Do NOT route through UIManager/ToolManager — the brush is a mode
        /// overlay, not a regular tool.
        /// </summary>
        public void ToggleBrush()
        {
            // Cannot activate brush without a block or plow selected.
            bool canActivate = _hasBuildTool
                            || (_toolManager != null && _toolManager.CurrentTool == ToolType.Tool_Plow)
                            || (_toolManager != null && _toolManager.CurrentTool == ToolType.Tool_Destroy);

            if (!IsBrushActive && !canActivate)
            {
                Debug.Log("[BrushTool] Brush blocked — no usable tool selected.");
                return;
            }

            IsBrushActive = !IsBrushActive;
            OnBrushToggled?.Invoke(IsBrushActive);
            _uiAudio?.PlayToggle();
            Debug.Log($"[BrushTool] Brush mode {(IsBrushActive ? "ON" : "OFF")}.");
        }

        #endregion

        #region Internals ?????????????????????????????????????

        /// <summary>
        /// Tracks the last build tool when the player switches blocks.
        /// Changing to Destroy or any non-build tool does NOT deactivate the brush.
        /// </summary>
        private void HandleToolChanged(ToolType newTool)
        {
            if (_toolManager.IsBuildTool)
            {
                _lastBuildTool = newTool;
                _hasBuildTool  = true;
            }

            // Tool_None selected — force brush OFF and keep button dimmed.
            if (newTool == ToolType.Tool_None)
            {
                IsBrushActive = false;
                OnBrushToggled?.Invoke(IsBrushActive);
                Debug.Log("[BrushTool] Tool_None selected — brush deactivated.");
            }
        }

        /// <summary>
        /// If the ToolManager is currently on a non-build tool (e.g. Destroy),
        /// silently restore the saved build tool so placement works.
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
