// ??????????????????????????????????????????????
//  BrushTool.cs  ·  _Project.Scripts.Interaction
//  Continuous block-painting while the finger is held and dragged.
//  Hooks into ARBlockPlacer for all placement logic — zero duplication.
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Brush (paint) tool — hold and drag to place a continuous trail of blocks.<br/>
    /// The brush is a <b>mode toggle</b> independent of the active tool slot:<br/>
    /// • Changing block type keeps the brush ON.<br/>
    /// • Selecting the Destroy tool keeps the brush ON (destroy + brush coexist).<br/>
    /// • The brush button dims when OFF and lights up when ON.<br/>
    /// Attach to the <c>XR Origin (Mobile AR)</c> GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Brush Tool")]
    public class BrushTool : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("ARBlockPlacer on the same GameObject — all placement is delegated here.")]
        [SerializeField] private ARBlockPlacer _blockPlacer;

        [Tooltip("ToolManager — listens for tool changes to track the last build tool.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("PlowTool — receives continuous placement calls when Tool_Plow is active.")]
        [SerializeField] private PlowTool _plowTool;

        [Header("Button Visual")]
        [Tooltip("Image on Btn_Brush that is dimmed when brush is OFF and full-bright when ON.")]
        [SerializeField] private Image _brushButtonImage;

        [Tooltip("Brightness multiplier applied to the button colour when brush is OFF. 0.45 = clearly dimmed.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dimFactor = 0.45f;

        [Header("Brush Settings")]
        [Tooltip("Minimum seconds between consecutive block placements while dragging.\n" +
                 "0.08 s ? one block per 5 frames at 60 fps. Raise for sparser trails.")]
        [SerializeField] private float _strokeCooldown = 0.08f;

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

        private Color _buttonOriginalColor;
        private float _lastPlaceTime   = -999f;
        private float _lastDestroyTime = -999f;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            if (_brushButtonImage != null)
                _buttonOriginalColor = _brushButtonImage.color;
        }

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

            // Start dimmed (brush OFF by default).
            RefreshButtonVisual();
        }

        private void Update()
        {
            if (!IsBrushActive) return;
            if (_blockPlacer == null) return;
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

            // ?? Destroy mode: mine continuously while finger is held ??????????
            if (_toolManager.CurrentTool == ToolType.Tool_Destroy)
            {
                if (Time.time - _lastDestroyTime < _strokeCooldown) return;
                _blockPlacer.TryDestroyBlock(touch.screenPosition);
                _lastDestroyTime = Time.time;
                return;
            }

            // ?? Plow mode: scatter pebbles continuously (no grid alignment) ???
            if (_toolManager.CurrentTool == ToolType.Tool_Plow)
            {
                _plowTool?.PlacePebbleAtScreen(touch.screenPosition);
                return;
            }

            // ?? Build mode: paint blocks continuously ?????????????????????????
            if (Time.time - _lastPlaceTime < _strokeCooldown) return;

            EnsureBuildToolActive();
            _blockPlacer.TryPlaceBlock(touch.screenPosition);
            _lastPlaceTime = Time.time;
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Toggles brush mode ON or OFF.<br/>
        /// Wire this to the <c>Btn_Brush</c> OnClick event in the Inspector.
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
            RefreshButtonVisual();
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
                RefreshButtonVisual();
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

        /// <summary>
        /// Dims the brush button when OFF, restores full colour when ON.
        /// </summary>
        private void RefreshButtonVisual()
        {
            if (_brushButtonImage == null) return;

            _brushButtonImage.color = IsBrushActive
                ? _buttonOriginalColor
                : _buttonOriginalColor * new Color(_dimFactor, _dimFactor, _dimFactor, 1f);
        }

        private void ValidateReferences()
        {
            if (_blockPlacer == null)
                Debug.LogError("[BrushTool] _blockPlacer is not assigned!", this);
            if (_toolManager == null)
                Debug.LogError("[BrushTool] _toolManager is not assigned!", this);
            if (_brushButtonImage == null)
                Debug.LogWarning("[BrushTool] _brushButtonImage is not assigned — button will not dim.", this);
        }

        #endregion
    }
}
