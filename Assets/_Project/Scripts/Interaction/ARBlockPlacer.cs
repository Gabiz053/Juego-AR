// ──────────────────────────────────────────────
//  ARBlockPlacer.cs  ·  _Project.Scripts.Interaction
//  AR touch interaction: places and destroys voxel blocks via
//  AR plane raycasts and physics raycasts.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using _Project.Scripts.Voxel;
using _Project.Scripts.Core;
using _Project.Scripts.AR;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Core AR interaction system — converts touch input into block
    /// placement or destruction using a combination of AR plane raycasts
    /// (first block on ground) and 3D physics raycasts (stacking on
    /// existing blocks).<br/>
    /// Delegates audio to <see cref="GameAudioService"/> and debug
    /// visualisation to <see cref="DebugRayVisualizer"/>.<br/>
    /// Attach to the <c>XR Origin (Mobile AR)</c> GameObject.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/AR Block Placer")]
    public class ARBlockPlacer : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("ToolManager — provides the currently selected tool and block prefab.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("GridManager — provides grid snapping and grid size.")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("ARWorldManager — manages the AR world anchor.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("Transform that parents all placed blocks (WorldContainer).")]
        [SerializeField] private Transform _worldContainer;

        [Header("Services")]
        [Tooltip("Centralised audio service for playing block SFX.")]
        [SerializeField] private GameAudioService _audioService;

        [Tooltip("Optional debug ray drawn on each tap. Can be null to disable.")]
        [SerializeField] private DebugRayVisualizer _debugRayVisualizer;

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for existing voxel blocks used by physics raycasts.")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Maximum distance (metres) from the camera at which blocks can be placed.")]
        [SerializeField] private float _maxBuildDistance = 7f;

        [Tooltip("Slight shrinkage applied to the overlap check box to avoid false positives at block edges.")]
        [SerializeField] private float _overlapTolerance = 0.05f;

        [Header("Game Feel")]
        [Tooltip("Particle system prefab spawned at the placement position when a block is placed.")]
        [SerializeField] private ParticleSystem _placeVfxPrefab;

        #endregion

        #region Cached Components ─────────────────────────────

        private ARRaycastManager _arRaycastManager;
        private Camera _mainCamera;

        /// <summary>Reusable list for AR raycast results — avoids GC alloc per frame.</summary>
        private readonly List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Awake()
        {
            _arRaycastManager = GetComponent<ARRaycastManager>();
            _mainCamera       = Camera.main;

            Debug.Log("[ARBlockPlacer] Awake — components cached.");
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Debug.Log("[ARBlockPlacer] Enhanced touch enabled.");
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            Debug.Log("[ARBlockPlacer] Enhanced touch disabled.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[ARBlockPlacer] Initialized.");
        }

        private void Update()
        {
            if (Touch.activeTouches.Count == 0) return;

            Touch touch = Touch.activeTouches[0];
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) return;

            // Ignore touches over UI elements (buttons, panels, etc.).
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.touchId))
            {
                return;
            }

            HandleTouch(touch.screenPosition);
        }

        #endregion

        #region Touch Dispatch ─────────────────────────────────

        /// <summary>
        /// Routes the touch to the correct handler based on the active tool.
        /// </summary>
        private void HandleTouch(Vector2 screenPosition)
        {
            if (_toolManager == null) return;

            // Delegate debug visualisation to the optional ray visualizer.
            if (_debugRayVisualizer != null)
                _debugRayVisualizer.ShowRay(screenPosition);

            if (_toolManager.IsBuildTool)
            {
                TryPlaceBlock(screenPosition);
            }
            else if (_toolManager.CurrentTool == ToolType.Tool_Destroy)
            {
                TryDestroyBlock(screenPosition);
            }
            else
            {
                Debug.Log($"[ARBlockPlacer] Touch ignored — tool {_toolManager.CurrentTool} has no placement/destroy action.");
            }
        }

        #endregion

        #region Block Placement ────────────────────────────────

        /// <summary>
        /// Attempts to place a block. First tries a physics raycast against
        /// existing blocks (stacking), then falls back to an AR plane raycast
        /// (first block on detected surface).
        /// </summary>
        private void TryPlaceBlock(Vector2 screenPosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            // 1. Try stacking on an existing block via physics raycast.
            if (Physics.Raycast(ray, out RaycastHit physHit, _maxBuildDistance, _voxelLayerMask))
            {
                Vector3 localNormal = _worldContainer.InverseTransformDirection(physHit.normal);
                Vector3 localHitPos = _worldContainer.InverseTransformPoint(physHit.transform.position);
                Vector3 rawLocalPos = localHitPos + (localNormal * _gridManager.GridSize);

                Debug.Log($"[ARBlockPlacer] Physics hit on {physHit.transform.name} — stacking block.");
                ProcessAndPlace(rawLocalPos);
                return;
            }

            // 2. Fall back to AR plane raycast (ground placement).
            if (_arRaycastManager.Raycast(screenPosition, _arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = _arHits[0].pose;

                // Distance check — ignore taps too far from the camera.
                float sqrDist = (hitPose.position - _mainCamera.transform.position).sqrMagnitude;
                if (sqrDist > _maxBuildDistance * _maxBuildDistance)
                {
                    Debug.Log("[ARBlockPlacer] AR hit too far — ignoring.");
                    return;
                }

                // Anchor the world on the first valid AR hit.
                if (!_arWorldManager.IsWorldAnchored)
                {
                    _arWorldManager.AnchorWorld(hitPose, _mainCamera.transform);
                    Debug.Log("[ARBlockPlacer] World anchored at AR hit pose.");
                }

                Vector3 rawLocalPos = _worldContainer.InverseTransformPoint(hitPose.position);

                // Snap vertical position to grid.
                float gridSize = _gridManager.GridSize;
                rawLocalPos.y = Mathf.Round(rawLocalPos.y / gridSize) * gridSize;

                Debug.Log("[ARBlockPlacer] AR plane hit — placing block on ground.");
                ProcessAndPlace(rawLocalPos);
            }
        }

        /// <summary>
        /// Snaps the raw local position, validates placement constraints,
        /// instantiates the block prefab, spawns VFX, and plays audio.
        /// </summary>
        private void ProcessAndPlace(Vector3 rawLocalPosition)
        {
            Vector3 snappedLocal = _gridManager.GetSnappedPosition(rawLocalPosition);
            Vector3 worldPos    = _worldContainer.TransformPoint(snappedLocal);
            float   worldScale  = _worldContainer.localScale.x;

            // Prevent placing a block on top of the camera.
            if (IsCameraInsideVoxel(worldPos, worldScale))
            {
                Debug.Log("[ARBlockPlacer] Placement blocked — camera is inside the target voxel.");
                return;
            }

            // Prevent overlapping with existing blocks.
            if (!IsSpaceEmpty(worldPos, worldScale))
            {
                Debug.Log("[ARBlockPlacer] Placement blocked — space is already occupied.");
                return;
            }

            // Get the prefab from ToolManager → BlockDatabase.
            GameObject prefab = _toolManager.GetCurrentBlockPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[ARBlockPlacer] No prefab returned for current tool — cannot place.");
                return;
            }

            // Instantiate block as child of WorldContainer.
            GameObject newBlock = Instantiate(prefab, _worldContainer);
            newBlock.transform.SetLocalPositionAndRotation(snappedLocal, Quaternion.identity);

            Debug.Log($"[ARBlockPlacer] Block placed: {prefab.name} at local {snappedLocal}.");

            // Spawn placement VFX.
            if (_placeVfxPrefab != null)
            {
                Instantiate(_placeVfxPrefab, worldPos, Quaternion.identity);
            }

            // Play placement audio via the audio service.
            VoxelBlock blockData = newBlock.GetComponent<VoxelBlock>();
            if (blockData != null && _audioService != null)
            {
                _audioService.PlayOneShot(blockData.PlaceSounds);
            }
        }

        #endregion

        #region Block Destruction ──────────────────────────────

        /// <summary>
        /// Casts a physics ray and destroys the first voxel block hit.
        /// </summary>
        private void TryDestroyBlock(Vector2 screenPosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _maxBuildDistance, _voxelLayerMask))
            {
                Debug.Log("[ARBlockPlacer] Destroy ray missed — no voxel hit.");
                return;
            }

            GameObject targetBlock = hit.transform.gameObject;

            // Play break audio before destruction via the audio service.
            VoxelBlock blockData = targetBlock.GetComponent<VoxelBlock>();
            if (blockData != null && _audioService != null)
            {
                _audioService.PlayOneShot(blockData.BreakSounds);
            }

            Destroy(targetBlock);
            Debug.Log($"[ARBlockPlacer] Block destroyed: {targetBlock.name}.");
        }

        #endregion

        #region Spatial Helpers ────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if the camera position falls within the
        /// voxel bounds at <paramref name="worldPos"/>.
        /// </summary>
        private bool IsCameraInsideVoxel(Vector3 worldPos, float worldScale)
        {
            float scaledSize = _gridManager.GridSize * worldScale;
            return new Bounds(worldPos, Vector3.one * scaledSize)
                       .Contains(_mainCamera.transform.position);
        }

        /// <summary>
        /// Returns <c>true</c> if no existing collider occupies the space
        /// at <paramref name="worldPos"/> (with a small tolerance margin).
        /// </summary>
        private bool IsSpaceEmpty(Vector3 worldPos, float worldScale)
        {
            float halfSize = ((_gridManager.GridSize * worldScale) / 2f) - _overlapTolerance;
            return !Physics.CheckBox(worldPos, Vector3.one * halfSize, Quaternion.identity, _voxelLayerMask);
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[ARBlockPlacer] _toolManager is not assigned!", this);
            if (_gridManager == null)
                Debug.LogError("[ARBlockPlacer] _gridManager is not assigned!", this);
            if (_arWorldManager == null)
                Debug.LogError("[ARBlockPlacer] _arWorldManager is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogError("[ARBlockPlacer] _worldContainer is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[ARBlockPlacer] Main Camera not found!", this);
            if (_audioService == null)
                Debug.LogWarning("[ARBlockPlacer] _audioService is not assigned — block sounds will be silent.", this);
            if (_debugRayVisualizer == null)
                Debug.LogWarning("[ARBlockPlacer] _debugRayVisualizer is not assigned — no debug ray.", this);
            if (_placeVfxPrefab == null)
                Debug.LogWarning("[ARBlockPlacer] _placeVfxPrefab is not assigned — no placement VFX.", this);
        }

        #endregion
    }
}
