// ------------------------------------------------------------
//  ARBlockPlacer.cs  -  _Project.Scripts.Interaction
//  Voxel block placement via AR plane and physics raycasts.
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using _Project.Scripts.AR;
using _Project.Scripts.Core;
using _Project.Scripts.Infrastructure;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Places voxel blocks on AR surfaces and on top of existing blocks.
    /// Uses AR plane raycasts (ground) and physics raycasts (stacking).
    /// Placement feedback (audio + VFX) is handled by each prefab's
    /// <see cref="BlockSpawn"/> component � this script only handles
    /// positioning, validation and game-logic notifications.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/AR Block Placer")]
    public class ARBlockPlacer : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ARWorldManager -- manages the AR world anchor.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("WorldContainer transform that parents all placed blocks.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("ARRaycastManager from the XR Origin.")]
        [SerializeField] private ARRaycastManager _arRaycastManager;

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for existing voxel blocks (physics raycasts).")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Maximum build distance from the camera (metres).")]
        [SerializeField] private float _maxBuildDistance = 7f;

        [Tooltip("Minimum distance from camera to prevent lens-level placement.")]
        [SerializeField] private float _minPlaceDistance = 0.4f;

        [Tooltip("Shrinkage applied to overlap check to avoid false positives at edges.")]
        [SerializeField] private float _overlapTolerance = 0.05f;

        #endregion

        #region State ---------------------------------------------

        private Camera _mainCamera;
        private IToolManager     _toolManager;
        private IGridManager     _gridManager;
        private IUndoRedoService _undoRedoService;
        private readonly List<ARRaycastHit> _arHits      = new List<ARRaycastHit>();
        private readonly HashSet<Vector3>   _pendingCells = new HashSet<Vector3>();

        #endregion

        #region Public API ----------------------------------------

        /// <summary>
        /// Attempts to place a block.  Tries physics raycast first (stacking),
        /// then falls back to AR plane raycast (ground).
        /// </summary>
        public void TryPlaceBlock(Vector2 screenPosition)
        {
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            // 1. Stacking on existing block
            if (Physics.Raycast(ray, out RaycastHit physHit, _maxBuildDistance, _voxelLayerMask))
            {
                // Resolve root block in case a child collider was hit.
                Transform hitRoot = physHit.transform;
                VoxelBlock vb     = physHit.transform.GetComponentInParent<VoxelBlock>();
                if (vb != null) hitRoot = vb.transform;

                Vector3 localNormal = _worldContainer.InverseTransformDirection(physHit.normal);
                Vector3 localHitPos = _worldContainer.InverseTransformPoint(hitRoot.position);
                Vector3 rawLocalPos = localHitPos + localNormal * _gridManager.GridSize;

                ProcessAndPlace(rawLocalPos);
                return;
            }

            // 2. Ground placement via AR plane
            if (_arRaycastManager != null &&
                _arRaycastManager.Raycast(screenPosition, _arHits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = _arHits[0].pose;

                float sqrDist = (hitPose.position - _mainCamera.transform.position).sqrMagnitude;
                if (sqrDist > _maxBuildDistance * _maxBuildDistance) return;

                if (!_arWorldManager.IsWorldAnchored)
                    _arWorldManager.AnchorWorld(hitPose, _mainCamera.transform);

                Vector3 rawLocalPos = _worldContainer.InverseTransformPoint(hitPose.position);
                float gridSize      = _gridManager.GridSize;
                rawLocalPos.y       = Mathf.Round(rawLocalPos.y / gridSize) * gridSize;

                ProcessAndPlace(rawLocalPos);
            }
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _mainCamera = Camera.main;
            ServiceLocator.TryGet<IToolManager>(out _toolManager);
            ServiceLocator.TryGet<IGridManager>(out _gridManager);
            ServiceLocator.TryGet<IUndoRedoService>(out _undoRedoService);
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Snaps position, validates, instantiates the block and starts
        /// its spawn animation.  Audio and VFX are handled by the prefab's
        /// <see cref="BlockSpawn"/> component.
        /// </summary>
        private void ProcessAndPlace(Vector3 rawLocalPosition)
        {
            Vector3 snappedLocal = _gridManager.GetSnappedPosition(rawLocalPosition);
            Vector3 worldPos     = _worldContainer.TransformPoint(snappedLocal);
            float   worldScale   = _worldContainer.localScale.x;

            float sqrDistToCamera = (worldPos - _mainCamera.transform.position).sqrMagnitude;
            if (WorldModeContext.Selected != WorldMode.Bonsai &&
                sqrDistToCamera < _minPlaceDistance * _minPlaceDistance) return;
            if (IsCameraInsideVoxel(worldPos, worldScale)) return;
            if (!IsSpaceEmpty(worldPos, worldScale) || _pendingCells.Contains(snappedLocal)) return;

            GameObject prefab = _toolManager.GetCurrentBlockPrefab();
            if (prefab == null) return;

            _pendingCells.Add(snappedLocal);

            GameObject newBlock = Instantiate(prefab, _worldContainer);
            newBlock.transform.SetLocalPositionAndRotation(snappedLocal, Quaternion.identity);

            BlockDestroy blockDestroy = newBlock.GetComponent<BlockDestroy>();

            BlockSpawn blockSpawn = newBlock.GetComponent<BlockSpawn>();
            if (blockSpawn != null)
            {
                blockSpawn.Play(_mainCamera.transform, () => _pendingCells.Remove(snappedLocal));
            }
            else
            {
                _pendingCells.Remove(snappedLocal);
                if (blockDestroy != null) blockDestroy.SetReady();
            }

            _undoRedoService?.Record(new PlaceBlockAction(
                newBlock, prefab, _worldContainer,
                snappedLocal, Quaternion.identity));

            VoxelBlock blockData = newBlock.GetComponent<VoxelBlock>();
            if (blockData != null)
                EventBus.Publish(new BlockPlacedEvent(
                    Vector3Int.RoundToInt(snappedLocal), blockData.Type));

            Debug.Log($"[ARBlockPlacer] Placed {prefab.name} at local {snappedLocal}.");
        }

        /// <summary>
        /// Returns <c>true</c> when the camera position falls inside
        /// the axis-aligned bounds of the voxel at <paramref name="worldPos"/>.
        /// </summary>
        private bool IsCameraInsideVoxel(Vector3 worldPos, float worldScale)
        {
            float scaledSize = _gridManager.GridSize * worldScale;
            return new Bounds(worldPos, Vector3.one * scaledSize)
                       .Contains(_mainCamera.transform.position);
        }

        /// <summary>
        /// Returns <c>true</c> when no existing collider overlaps the
        /// target cell.  Uses <see cref="Physics.CheckBox"/> with a
        /// tolerance shrink to avoid false positives at face-adjacent edges.
        /// </summary>
        private bool IsSpaceEmpty(Vector3 worldPos, float worldScale)
        {
            float halfSize = _gridManager.GridSize * worldScale / 2f - _overlapTolerance;
            return !Physics.CheckBox(worldPos, Vector3.one * halfSize, Quaternion.identity, _voxelLayerMask);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogWarning("[ARBlockPlacer] _toolManager is not assigned.", this);
            if (_gridManager == null)
                Debug.LogWarning("[ARBlockPlacer] _gridManager is not assigned.", this);
            if (_arWorldManager == null)
                Debug.LogWarning("[ARBlockPlacer] _arWorldManager is not assigned.", this);
            if (_worldContainer == null)
                Debug.LogWarning("[ARBlockPlacer] _worldContainer is not assigned.", this);
            if (_arRaycastManager == null)
                Debug.LogWarning("[ARBlockPlacer] _arRaycastManager is not assigned.", this);
            if (_mainCamera == null)
                Debug.LogWarning("[ARBlockPlacer] _mainCamera is not assigned.", this);
        }

        #endregion
    }
}
