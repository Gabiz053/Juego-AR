// ------------------------------------------------------------
//  ARBlockPlacer.cs  -  _Project.Scripts.Interaction
//  Handles voxel block placement via AR plane raycasts and
//  physics raycasts (stacking on existing blocks).
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using _Project.Scripts.AR;
using _Project.Scripts.Core;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Places voxel blocks on AR surfaces and on top of existing blocks.
    /// Uses AR plane raycasts (ground) and physics raycasts (stacking).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/AR Block Placer")]
    public class ARBlockPlacer : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ToolManager -- provides the current tool and block prefab.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("GridManager -- provides grid snapping and grid size.")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("ARWorldManager -- manages the AR world anchor.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("WorldContainer transform that parents all placed blocks.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("ARRaycastManager from the XR Origin.")]
        [SerializeField] private ARRaycastManager _arRaycastManager;

        [Header("Services")]
        [Tooltip("Centralised audio service for block SFX.")]
        [SerializeField] private GameAudioService _audioService;

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for existing voxel blocks (physics raycasts).")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Maximum build distance from the camera (metres).")]
        [SerializeField] private float _maxBuildDistance = 7f;

        [Tooltip("Minimum distance from camera to prevent lens-level placement.")]
        [SerializeField] private float _minPlaceDistance = 0.4f;

        [Tooltip("Shrinkage applied to overlap check to avoid false positives at edges.")]
        [SerializeField] private float _overlapTolerance = 0.05f;

        [Header("Game Feel")]
        [Tooltip("VFX prefab spawned at placement position.")]
        [SerializeField] private GameObject _placeVfxPrefab;

        [Tooltip("VFX prefab injected into each block for break effects.")]
        [SerializeField] private GameObject _breakVfxPrefab;

        [Header("Undo / Redo")]
        [Tooltip("UndoRedoService -- records every placement.")]
        [SerializeField] private UndoRedoService _undoRedoService;

        [Header("Harmony")]
        [Tooltip("HarmonyService -- notified on every block placed.")]
        [SerializeField] private HarmonyService _harmonyService;

        #endregion

        #region State ---------------------------------------------

        private Camera _mainCamera;
        private readonly List<ARRaycastHit> _arHits      = new List<ARRaycastHit>();
        private readonly HashSet<Vector3>   _pendingCells = new HashSet<Vector3>();

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            ValidateReferences();
        }

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
                Vector3 localNormal = _worldContainer.InverseTransformDirection(physHit.normal);
                Vector3 localHitPos = _worldContainer.InverseTransformPoint(physHit.transform.position);
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

        /// <summary>
        /// Snaps position, validates, instantiates the block, plays VFX and audio.
        /// </summary>
        public void ProcessAndPlace(Vector3 rawLocalPosition)
        {
            Vector3 snappedLocal = _gridManager.GetSnappedPosition(rawLocalPosition);
            Vector3 worldPos     = _worldContainer.TransformPoint(snappedLocal);
            float   worldScale   = _worldContainer.localScale.x;

            if (Vector3.Distance(worldPos, _mainCamera.transform.position) < _minPlaceDistance) return;
            if (IsCameraInsideVoxel(worldPos, worldScale)) return;
            if (!IsSpaceEmpty(worldPos, worldScale) || _pendingCells.Contains(snappedLocal)) return;

            GameObject prefab = _toolManager.GetCurrentBlockPrefab();
            if (prefab == null) return;

            _pendingCells.Add(snappedLocal);

            GameObject newBlock = Instantiate(prefab, _worldContainer);
            newBlock.transform.SetLocalPositionAndRotation(snappedLocal, Quaternion.identity);

            BlockDestroy blockDestroy = newBlock.GetComponent<BlockDestroy>();
            if (blockDestroy != null)
                blockDestroy.InjectSharedRefs(_breakVfxPrefab, _audioService);

            void ArmBlock(GameObject instance)
            {
                BlockDestroy bd = instance.GetComponent<BlockDestroy>();
                if (bd != null)
                {
                    bd.InjectSharedRefs(_breakVfxPrefab, _audioService);
                    bd.SetReady();
                }
            }

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
                snappedLocal, Quaternion.identity,
                _breakVfxPrefab, _audioService, ArmBlock));

            VoxelBlock blockData = newBlock.GetComponent<VoxelBlock>();
            if (blockData != null)
                _harmonyService?.NotifyBlockPlaced(blockData.Type);

            if (_placeVfxPrefab != null)
                Instantiate(_placeVfxPrefab, worldPos, Quaternion.identity);

            if (blockData != null && _audioService != null)
                _audioService.PlayOneShot(blockData.PlaceSounds);
        }

        #endregion

        #region Internals -----------------------------------------

        private bool IsCameraInsideVoxel(Vector3 worldPos, float worldScale)
        {
            float scaledSize = _gridManager.GridSize * worldScale;
            return new Bounds(worldPos, Vector3.one * scaledSize)
                       .Contains(_mainCamera.transform.position);
        }

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
                Debug.LogError("[ARBlockPlacer] Camera.main not found!", this);
        }

        #endregion
    }
}
