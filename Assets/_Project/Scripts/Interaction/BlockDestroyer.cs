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
    /// Break feedback (audio + VFX) is handled by each prefab's
    /// <see cref="BlockDestroy"/> component.
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

        [Header("Voxel Settings")]
        [Tooltip("Layer mask for voxel block physics raycasts.")]
        [SerializeField] private LayerMask _voxelLayerMask;

        [Tooltip("Layer mask for pebble objects.")]
        [SerializeField] private LayerMask _pebbleLayerMask;

        [Tooltip("Maximum destroy raycast distance (metres).")]
        [SerializeField] private float _maxDestroyDistance = 7f;

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

        /// <summary>
        /// Processes a confirmed raycast hit.  Resolves the root block,
        /// skips blocks already mid-destruction, records undo, triggers
        /// the physics-tumble sequence, and notifies Harmony.
        /// </summary>
        private void DestroyHit(RaycastHit hit)
        {
            // Resolve root block — child colliders point to the parent VoxelBlock.
            VoxelBlock blockData = hit.transform.GetComponentInParent<VoxelBlock>();
            GameObject target    = blockData != null
                ? blockData.gameObject
                : hit.transform.gameObject;

            // Skip blocks that are already being destroyed (proximity knock
            // may have triggered in the same frame via BlockDestroy.Update).
            BlockDestroy blockDestroy = target.GetComponent<BlockDestroy>();
            if (blockDestroy != null && blockDestroy.IsKnocked)
                return;

            // Record undo BEFORE triggering KnockRoutine (which unparents).
            if (blockData != null && _undoRedoService != null)
            {
                GameObject prefab = _toolManager.GetBlockPrefab(blockData.Type);
                if (prefab != null)
                {
                    Vector3 localPos = _worldContainer.InverseTransformPoint(target.transform.position);

                    _undoRedoService.Record(new DestroyBlockAction(
                        prefab, _worldContainer, localPos, Quaternion.identity));

                    Debug.Log($"[BlockDestroyer] Destroy {blockData.Type} at local {localPos}.");
                }
            }

            // Trigger destruction (physics tumble + audio + VFX).
            if (blockDestroy != null)
            {
                blockDestroy.BreakFromTool(hit.normal);
            }
            else
            {
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
        }

        #endregion
    }
}
