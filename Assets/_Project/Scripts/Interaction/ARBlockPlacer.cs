// ──────────────────────────────────────────────
//  ARBlockPlacer.cs  ·  _Project.Scripts.Interaction
//  Handles voxel block placement via AR plane raycasts and
//  physics raycasts (stacking on existing blocks).
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using _Project.Scripts.Voxel;
using _Project.Scripts.Core;
using _Project.Scripts.AR;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Places voxel blocks on AR surfaces and on top of existing blocks.<br/>
    /// Uses a combination of AR plane raycasts (first block on ground)
    /// and 3D physics raycasts (stacking).<br/>
    /// Called by <see cref="TouchInputRouter"/> on single taps and by
    /// <see cref="BrushTool"/> during continuous painting.<br/>
    /// Delegates audio to <see cref="GameAudioService"/>.
    /// </summary>
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

        [Tooltip("ARRaycastManager — used for AR plane raycasts. Assign from the XR Origin GameObject.")]
        [SerializeField] private ARRaycastManager _arRaycastManager;

        [Header("Services")]
        [Tooltip("Centralised audio service for playing block SFX.")]
        [SerializeField] private GameAudioService _audioService;

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for existing voxel blocks used by physics raycasts.")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Maximum distance (metres) from the camera at which blocks can be placed.")]
        [SerializeField] private float _maxBuildDistance = 7f;

        [Tooltip("Minimum distance (metres) from the camera required to place a block. Prevents placing blocks right in front of the lens.")]
        [SerializeField] private float _minPlaceDistance = 0.4f;

        [Tooltip("Slight shrinkage applied to the overlap check box to avoid false positives at block edges.")]
        [SerializeField] private float _overlapTolerance = 0.05f;

        [Header("Game Feel")]
        [Tooltip("VFX prefab spawned at the placement position when a block is placed.")]
        [SerializeField] private GameObject _placeVfxPrefab;

        [Tooltip("VFXBlockDestroy prefab — injected into each block's BlockDestroy component for break VFX.")]
        [SerializeField] private GameObject _breakVfxPrefab;

        [Header("Undo / Redo")]
        [Tooltip("UndoRedoService — records every place action so it can be reversed.")]
        [SerializeField] private UndoRedoService _undoRedoService;

        [Header("Harmony")]
        [Tooltip("HarmonyService — notified on every block place.")]
        [SerializeField] private HarmonyService _harmonyService;

        #endregion

        #region Cached Components ─────────────────────────────

        private Camera _mainCamera;

        /// <summary>Reusable list for AR raycast results — avoids GC alloc per frame.</summary>
        private readonly List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

        /// <summary>
        /// Grid cells reserved by blocks currently mid-spawn-animation (collider still disabled).
        /// Prevents rapid taps from placing a second block in the same cell before the
        /// first block's collider is live.
        /// </summary>
        private readonly HashSet<Vector3> _pendingCells = new HashSet<Vector3>();

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Awake()
        {
            _mainCamera = Camera.main;

            Debug.Log("[ARBlockPlacer] Awake — components cached.");
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[ARBlockPlacer] Initialized.");
        }

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Attempts to place a block. First tries a physics raycast against
        /// existing blocks (stacking), then falls back to an AR plane raycast
        /// (first block on detected surface).<br/>
        /// Called by <see cref="TouchInputRouter"/> and
        /// <see cref="BrushTool"/> for continuous painting.
        /// </summary>
        public void TryPlaceBlock(Vector2 screenPosition)
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
            if (_arRaycastManager != null &&
                _arRaycastManager.Raycast(screenPosition, _arHits, TrackableType.PlaneWithinPolygon))
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
        /// instantiates the block prefab, spawns VFX, and plays audio.<br/>
        /// Called by <see cref="BrushTool"/> as well as <see cref="TryPlaceBlock"/>.
        /// </summary>
        public void ProcessAndPlace(Vector3 rawLocalPosition)
        {
            Vector3 snappedLocal = _gridManager.GetSnappedPosition(rawLocalPosition);
            Vector3 worldPos    = _worldContainer.TransformPoint(snappedLocal);
            float   worldScale  = _worldContainer.localScale.x;

            // Prevent placing a block too close to the camera.
            float distToCamera = Vector3.Distance(worldPos, _mainCamera.transform.position);
            if (distToCamera < _minPlaceDistance)
            {
                Debug.Log($"[ARBlockPlacer] Placement blocked — too close to camera ({distToCamera:F2}m < {_minPlaceDistance}m).");
                return;
            }

            // Prevent placing a block on top of the camera.
            if (IsCameraInsideVoxel(worldPos, worldScale))
            {
                Debug.Log("[ARBlockPlacer] Placement blocked — camera is inside the target voxel.");
                return;
            }

            // Prevent overlapping with existing blocks OR cells reserved mid-animation.
            if (!IsSpaceEmpty(worldPos, worldScale) || _pendingCells.Contains(snappedLocal))
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

            // Reserve the cell immediately so rapid taps can't double-place.
            _pendingCells.Add(snappedLocal);

            // Instantiate block as child of WorldContainer.
            GameObject newBlock = Instantiate(prefab, _worldContainer);
            newBlock.transform.SetLocalPositionAndRotation(snappedLocal, Quaternion.identity);

            // Forward shared refs to BlockDestroy (VFX + audio).
            BlockDestroy blockDestroy = newBlock.GetComponent<BlockDestroy>();
            if (blockDestroy != null)
                blockDestroy.InjectSharedRefs(_breakVfxPrefab, _audioService);

            // Callback that arms BlockDestroy — reused by undo/redo instantiation.
            void ArmBlock(GameObject instance)
            {
                BlockDestroy bd = instance.GetComponent<BlockDestroy>();
                if (bd != null)
                {
                    bd.InjectSharedRefs(_breakVfxPrefab, _audioService);
                    bd.SetReady();
                }
            }

            // Trigger spawn animation — releases the pending cell when done.
            BlockSpawn blockSpawn = newBlock.GetComponent<BlockSpawn>();
            if (blockSpawn != null)
            {
                blockSpawn.Play(_mainCamera.transform, () =>
                {
                    _pendingCells.Remove(snappedLocal);
                });
            }
            else
            {
                _pendingCells.Remove(snappedLocal);
                if (blockDestroy != null) blockDestroy.SetReady();
            }

            // Record for undo/redo AFTER the block exists.
            _undoRedoService?.Record(new PlaceBlockAction(
                newBlock, prefab, _worldContainer,
                snappedLocal, Quaternion.identity,
                _breakVfxPrefab, _audioService, ArmBlock));

            // Notify harmony — block type comes from the VoxelBlock component.
            VoxelBlock blockData = newBlock.GetComponent<VoxelBlock>();
            if (blockData != null)
                _harmonyService?.NotifyBlockPlaced(blockData.Type);

            Debug.Log($"[ARBlockPlacer] Block placed: {prefab.name} at local {snappedLocal}.");

            if (_placeVfxPrefab != null)
                Instantiate(_placeVfxPrefab, worldPos, Quaternion.identity);

            if (blockData != null && _audioService != null)
                _audioService.PlayOneShot(blockData.PlaceSounds);
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
            if (_arRaycastManager == null)
                Debug.LogError("[ARBlockPlacer] _arRaycastManager is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[ARBlockPlacer] Main Camera not found!", this);
            if (_audioService == null)
                Debug.LogWarning("[ARBlockPlacer] _audioService is not assigned — block sounds will be silent.", this);
            if (_placeVfxPrefab == null)
                Debug.LogWarning("[ARBlockPlacer] _placeVfxPrefab is not assigned — no placement VFX.", this);
        }

        #endregion
    }
}
