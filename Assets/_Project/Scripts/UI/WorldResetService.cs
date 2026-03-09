// ──────────────────────────────────────────────
//  WorldResetService.cs  ·  _Project.Scripts.UI
//  Encapsulates the "clear all blocks" world-reset operation.
// ──────────────────────────────────────────────

using System;
using UnityEngine;
using _Project.Scripts.AR;
using _Project.Scripts.Core;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Destroys every placed block, resets the AR spatial anchor, and
    /// deactivates the construction grid.  Extracted from
    /// <see cref="GameOptionsMenu"/> so the reset logic can be reused
    /// and tested independently of the UI layer.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/UI/World Reset Service")]
    public class WorldResetService : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("AR World References")]
        [Tooltip("Transform that parents all placed blocks (WorldContainer).")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("ARWorldManager — used to reset the AR spatial anchor.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("GridManager — used to deactivate the grid visual after clearing.")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("UndoRedoService — its stacks are cleared together with the world reset.")]
        [SerializeField] private UndoRedoService _undoRedoService;

        #endregion

        #region Events ────────────────────────────────────────

        /// <summary>
        /// Raised after the world has been fully reset (blocks destroyed,
        /// anchor cleared, grid hidden).  Other systems can subscribe to
        /// react — e.g. analytics, UI refresh, tutorial triggers.
        /// </summary>
        public event Action OnWorldReset;

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[WorldResetService] Initialized.");
        }

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>
        /// Executes the full world reset sequence:
        /// 1) Destroy all child blocks,
        /// 2) Reset the AR anchor,
        /// 3) Deactivate the grid,
        /// 4) Raise <see cref="OnWorldReset"/>.
        /// </summary>
        public void ResetWorld()
        {
            DestroyAllBlocks();
            ResetAnchor();
            DeactivateGrid();
            _undoRedoService?.Clear();

            OnWorldReset?.Invoke();
            Debug.Log("[WorldResetService] World fully reset (blocks + anchor + grid).");
        }

        /// <summary>
        /// Returns the current number of placed blocks under the world container.
        /// </summary>
        public int BlockCount => _worldContainer != null ? _worldContainer.childCount : 0;

        #endregion

        #region Internals ─────────────────────────────────────

        /// <summary>
        /// Destroys all child GameObjects under <see cref="_worldContainer"/>
        /// that have a <see cref="VoxelBlock"/> component, iterating in
        /// reverse to avoid index shifting. Infrastructure children
        /// (e.g. grid visual mesh) are left untouched.
        /// </summary>
        private void DestroyAllBlocks()
        {
            if (_worldContainer == null)
            {
                Debug.LogWarning("[WorldResetService] _worldContainer is null — cannot destroy blocks.", this);
                return;
            }

            int destroyed = 0;
            for (int i = _worldContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = _worldContainer.GetChild(i).gameObject;
                if (child.GetComponent<VoxelBlock>() != null)
                {
                    Destroy(child);
                    destroyed++;
                }
            }

            Debug.Log($"[WorldResetService] Destroyed {destroyed} block(s) from WorldContainer.");
        }

        /// <summary>
        /// Resets the AR spatial anchor so a new placement can be established.
        /// </summary>
        private void ResetAnchor()
        {
            if (_arWorldManager == null)
            {
                Debug.LogWarning("[WorldResetService] _arWorldManager is null — cannot reset anchor.", this);
                return;
            }

            _arWorldManager.ResetAnchor();
            Debug.Log("[WorldResetService] AR anchor reset.");
        }

        /// <summary>
        /// Deactivates the construction grid visual.
        /// </summary>
        private void DeactivateGrid()
        {
            if (_gridManager == null)
            {
                Debug.LogWarning("[WorldResetService] _gridManager is null — cannot deactivate grid.", this);
                return;
            }

            _gridManager.DeactivateGrid();
            Debug.Log("[WorldResetService] Grid deactivated.");
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_worldContainer == null)
                Debug.LogError("[WorldResetService] _worldContainer is not assigned!", this);
            if (_arWorldManager == null)
                Debug.LogError("[WorldResetService] _arWorldManager is not assigned!", this);
            if (_gridManager == null)
                Debug.LogError("[WorldResetService] _gridManager is not assigned!", this);
            if (_undoRedoService == null)
                Debug.LogError("[WorldResetService] _undoRedoService is not assigned!", this);
        }

        #endregion
    }
}
