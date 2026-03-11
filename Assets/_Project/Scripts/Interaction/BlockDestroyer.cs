// ------------------------------------------------------------
//  BlockDestroyer.cs  -  _Project.Scripts.Interaction
//  Handles block and pebble destruction via physics raycasts.
// ------------------------------------------------------------

using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Destroys voxel blocks and pebbles hit by a physics raycast.
    /// Records each destruction in <see cref="UndoRedoService"/> and
    /// notifies <see cref="HarmonyService"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Block Destroyer")]
    public class BlockDestroyer : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Dependencies")]
        [Tooltip("ToolManager -- looks up prefab for undo recording.")]
        [SerializeField] private ToolManager _toolManager;

        [Tooltip("WorldContainer transform that parents all placed blocks.")]
        [SerializeField] private Transform _worldContainer;

        [Header("Services")]
        [Tooltip("Centralised audio service for block SFX.")]
        [SerializeField] private GameAudioService _audioService;

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for voxel block physics raycasts.")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Layer mask for pebble objects.")]
        [SerializeField] private LayerMask _pebbleLayerMask;

        [Tooltip("Maximum destroy raycast distance (metres).")]
        [SerializeField] private float _maxDestroyDistance = 7f;

        [Header("Game Feel")]
        [Tooltip("VFX prefab spawned on destruction.")]
        [SerializeField] private GameObject _breakVfxPrefab;

        [Header("Undo / Redo")]
        [Tooltip("UndoRedoService -- records every destroy action.")]
        [SerializeField] private UndoRedoService _undoRedoService;

        [Header("Harmony")]
        [Tooltip("HarmonyService -- notified on every block destroy.")]
        [SerializeField] private HarmonyService _harmonyService;

        #endregion

        #region State ---------------------------------------------

        private Camera _mainCamera;

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
        /// Casts a physics ray and destroys the first voxel block or
        /// pebble hit.
        /// </summary>
        public void TryDestroyBlock(Vector2 screenPosition)
        {
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _maxDestroyDistance, _voxelLayerMask))
            {
                DestroyHit(hit);
                return;
            }

            if (_pebbleLayerMask != 0 &&
                Physics.Raycast(ray, out RaycastHit pebbleHit, _maxDestroyDistance, _pebbleLayerMask))
            {
                DestroyHit(pebbleHit);
            }
        }

        #endregion

        #region Internals -----------------------------------------

        private void DestroyHit(RaycastHit hit)
        {
            GameObject target    = hit.transform.gameObject;
            VoxelBlock blockData = target.GetComponent<VoxelBlock>();

            // Record undo action for voxel blocks
            if (blockData != null && _undoRedoService != null)
            {
                GameObject prefab = _toolManager.GetBlockPrefab(blockData.Type);
                if (prefab != null)
                {
                    Vector3 localPos = target.transform.localPosition;

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
                        prefab, _worldContainer, localPos, Quaternion.identity,
                        _breakVfxPrefab, _audioService, ArmBlock));
                }
            }

            // Trigger destruction
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

            if (blockData != null)
                _harmonyService?.NotifyBlockDestroyed(blockData.Type);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_toolManager == null)
                Debug.LogError("[BlockDestroyer] _toolManager is not assigned!", this);
            if (_worldContainer == null)
                Debug.LogError("[BlockDestroyer] _worldContainer is not assigned!", this);
            if (_mainCamera == null)
                Debug.LogError("[BlockDestroyer] Camera.main not found!", this);
            if (_audioService == null)
                Debug.LogWarning("[BlockDestroyer] _audioService is not assigned!", this);
        }

        #endregion
    }
}
