// ------------------------------------------------------------
//  TouchInputRouter.cs  -  _Project.Scripts.Interaction
//  Captures touch input, filters UI taps, and dispatches to the
//  appropriate handler (ARBlockPlacer, BlockDestroyer, BrushTool).
// ------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Single entry point for all touch input.  Routes touches to
    /// <see cref="ARBlockPlacer"/> or <see cref="BlockDestroyer"/>
    /// depending on the current tool.  Yields to <see cref="BrushTool"/>
    /// when active.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Touch Input Router")]
    public class TouchInputRouter : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ToolManager -- provides the currently selected tool.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("ARBlockPlacer -- receives placement requests.")]
        [SerializeField] private ARBlockPlacer _blockPlacer;

        [Tooltip("BlockDestroyer -- receives destruction requests.")]
        [SerializeField] private BlockDestroyer _blockDestroyer;

        [Header("Tools")]
        [Tooltip("BrushTool -- when active, input is yielded.")]
        [SerializeField] private BrushTool _brushTool;

        [Header("Debug")]
        [Tooltip("Optional debug ray drawn on each processed tap.")]
        [SerializeField] private DebugRayVisualizer _debugRayVisualizer;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void OnEnable()  => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private void Start()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) return;

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId))
                return;

            bool brushOwnsTouch = _brushTool != null
                               && _brushTool.IsBrushActive
                               && _toolManager != null
                               && _toolManager.CurrentTool != ToolType.Tool_Destroy;
            if (brushOwnsTouch) return;

            HandleTouch(touch.screenPosition);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Routes a validated screen touch to the correct handler based
        /// on the current <see cref="ToolManager.CurrentTool"/>.
        /// </summary>
        private void HandleTouch(Vector2 screenPosition)
        {
            if (_toolManager == null) return;

            if (_debugRayVisualizer != null)
                _debugRayVisualizer.ShowRay(screenPosition);

            if (_toolManager.IsBuildTool)
                _blockPlacer?.TryPlaceBlock(screenPosition);
            else if (_toolManager.CurrentTool == ToolType.Tool_Destroy)
                _blockDestroyer?.TryDestroyBlock(screenPosition);
        }

        #endregion

        #region Validation ----------------------------------------

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
