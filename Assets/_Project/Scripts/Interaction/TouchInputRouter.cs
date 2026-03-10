// ??????????????????????????????????????????????
//  TouchInputRouter.cs  ·  _Project.Scripts.Interaction
//  Captures touch input, filters UI taps, and dispatches to the
//  appropriate handler (ARBlockPlacer, BlockDestroyer, or BrushTool).
// ??????????????????????????????????????????????

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Single entry point for all touch input in the game.<br/>
    /// Reads <see cref="UnityEngine.InputSystem.EnhancedTouch.Touch"/>
    /// events each frame, filters out UI hits, and routes the touch
    /// to <see cref="ARBlockPlacer"/> or <see cref="BlockDestroyer"/>
    /// based on the current <see cref="ToolManager"/> selection.<br/>
    /// When <see cref="BrushTool"/> is active and the current tool is
    /// a build tool, input is yielded so the brush owns the touch.<br/>
    /// Also forwards screen position to the optional
    /// <see cref="DebugRayVisualizer"/> on every processed tap.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Touch Input Router")]
    public class TouchInputRouter : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("ToolManager — provides the currently selected tool.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("ARBlockPlacer — receives placement requests.")]
        [SerializeField] private ARBlockPlacer _blockPlacer;

        [Tooltip("BlockDestroyer — receives destruction requests.")]
        [SerializeField] private BlockDestroyer _blockDestroyer;

        [Header("Tools")]
        [Tooltip("Optional BrushTool — when active and a build tool is selected, input is yielded.")]
        [SerializeField] private BrushTool _brushTool;

        [Header("Debug")]
        [Tooltip("Optional debug ray drawn on each processed tap.")]
        [SerializeField] private DebugRayVisualizer _debugRayVisualizer;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[TouchInputRouter] Initialized.");
        }

        private void Update()
        {
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) return;

            // Ignore touches over UI elements (buttons, panels, etc.).
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId))
                return;

            // Yield to BrushTool only when brush is active AND we are NOT mining.
            // Destroy taps always pass through so the player can mine with brush ON.
            bool brushOwnsTouch = _brushTool != null
                               && _brushTool.IsBrushActive
                               && _toolManager != null
                               && _toolManager.CurrentTool != ToolType.Tool_Destroy;
            if (brushOwnsTouch) return;

            HandleTouch(touch.screenPosition);
        }

        #endregion

        #region Touch Dispatch ?????????????????????????????????

        private void HandleTouch(Vector2 screenPosition)
        {
            if (_toolManager == null) return;

            if (_debugRayVisualizer != null)
                _debugRayVisualizer.ShowRay(screenPosition);

            if (_toolManager.IsBuildTool)
            {
                _blockPlacer?.TryPlaceBlock(screenPosition);
            }
            else if (_toolManager.CurrentTool == ToolType.Tool_Destroy)
            {
                _blockDestroyer?.TryDestroyBlock(screenPosition);
            }
            else
            {
                Debug.Log($"[TouchInputRouter] Touch ignored — tool {_toolManager.CurrentTool} has no action.");
            }
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[TouchInputRouter] _toolManager is not assigned!", this);
            if (_blockPlacer == null)
                Debug.LogError("[TouchInputRouter] _blockPlacer is not assigned!", this);
            if (_blockDestroyer == null)
                Debug.LogError("[TouchInputRouter] _blockDestroyer is not assigned!", this);
        }

        #endregion
    }
}
