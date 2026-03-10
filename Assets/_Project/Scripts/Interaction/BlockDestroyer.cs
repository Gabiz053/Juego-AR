// ??????????????????????????????????????????????
//  BlockDestroyer.cs  ·  _Project.Scripts.Interaction
//  Handles block and pebble destruction via physics raycasts.
// ??????????????????????????????????????????????

using UnityEngine;
using _Project.Scripts.Voxel;
using _Project.Scripts.Core;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Destroys voxel blocks and pebbles hit by a physics raycast.<br/>
    /// Records each destruction as an <see cref="IUndoableAction"/> in
    /// <see cref="UndoRedoService"/> and notifies
    /// <see cref="HarmonyService"/>.<br/>
    /// Called by <see cref="TouchInputRouter"/> on single taps and by
    /// <see cref="BrushTool"/> during continuous mining.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Block Destroyer")]
    public class BlockDestroyer : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Dependencies")]
        [Tooltip("ToolManager — used to look up the prefab for undo recording.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("Transform that parents all placed blocks (WorldContainer).")]
        [SerializeField] private Transform _worldContainer;

        [Header("Services")]
        [Tooltip("Centralised audio service for playing block SFX.")]
        [SerializeField] private GameAudioService _audioService;

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for existing voxel blocks used by physics raycasts.")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Layer mask for pebble objects — included in destroy raycasts.")]
        [SerializeField] private LayerMask _pebbleLayerMask;

        [Tooltip("Maximum distance (metres) from the camera for destroy raycasts.")]
        [SerializeField] private float _maxDestroyDistance = 7f;

        [Header("Game Feel")]
        [Tooltip("VFXBlockDestroy prefab — dust particle burst spawned on destruction.")]
        [SerializeField] private GameObject _breakVfxPrefab;

        [Header("Undo / Redo")]
        [Tooltip("UndoRedoService — records every destroy action so it can be reversed.")]
        [SerializeField] private UndoRedoService _undoRedoService;

        [Header("Harmony")]
        [Tooltip("HarmonyService — notified on every block destroy.")]
        [SerializeField] private HarmonyService _harmonyService;

        #endregion

        #region Cached Components ?????????????????????????????

        private Camera _mainCamera;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[BlockDestroyer] Initialized.");
        }

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>
        /// Casts a physics ray from <paramref name="screenPosition"/> and
        /// destroys the first voxel block or pebble hit.<br/>
        /// Called by <see cref="TouchInputRouter"/> and
        /// <see cref="BrushTool"/> for continuous mining.
        /// </summary>
        public void TryDestroyBlock(Vector2 screenPosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            // 1. Try voxel blocks first.
            if (Physics.Raycast(ray, out RaycastHit hit, _maxDestroyDistance, _voxelLayerMask))
            {
                DestroyHit(hit);
                return;
            }

            // 2. Try pebbles — same ray, pebble layer mask.
            if (_pebbleLayerMask != 0 &&
                Physics.Raycast(ray, out RaycastHit pebbleHit, _maxDestroyDistance, _pebbleLayerMask))
            {
                DestroyHit(pebbleHit);
                return;
            }

            Debug.Log("[BlockDestroyer] Destroy ray missed — no voxel or pebble hit.");
        }

        #endregion

        #region Internals ?????????????????????????????????????

        private void DestroyHit(RaycastHit hit)
        {
            GameObject target = hit.transform.gameObject;

            VoxelBlock blockData = target.GetComponent<VoxelBlock>();
            if (blockData != null && _undoRedoService != null)
            {
                GameObject prefab = _toolManager.GetBlockPrefab(blockData.Type);

                if (prefab != null)
                {
                    Vector3    localPos = target.transform.localPosition;
                    Quaternion localRot = Quaternion.identity;

                    void ArmBlock(GameObject instance)
                    {
                        BlockDestroy bd = instance.GetComponent<BlockDestroy>();
                        if (bd != null)
                        {
                            bd.InjectSharedRefs(_breakVfxPrefab, _audioService);
                            bd.SetReady();
                        }
                    }

                    _undoRedoService.Record(new DestroyBlockAction(
                        prefab, _worldContainer, localPos, localRot,
                        _breakVfxPrefab, _audioService, ArmBlock));
                }
            }

            BlockDestroy blockDestroy = target.GetComponent<BlockDestroy>();
            if (blockDestroy != null)
            {
                blockDestroy.BreakFromTool(hit.normal);
            }
            else
            {
                if (blockData != null && _audioService != null)
                    _audioService.PlayOneShot(blockData.BreakSounds);

                if (_breakVfxPrefab != null)
                    Instantiate(_breakVfxPrefab, hit.transform.position, Quaternion.identity);

                Destroy(target);
            }

            // Notify harmony after the block has been removed.
            if (blockData != null)
                _harmonyService?.NotifyBlockDestroyed(blockData.Type);

            Debug.Log($"[BlockDestroyer] Destroyed: {target.name}.");
        }

        #endregion

        #region Validation ????????????????????????????????????

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[BlockDestroyer] _toolManager is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogError("[BlockDestroyer] _worldContainer is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[BlockDestroyer] Main Camera not found!", this);
            if (_audioService == null)
                Debug.LogWarning("[BlockDestroyer] _audioService is not assigned — block sounds will be silent.", this);
        }

        #endregion
    }
}
