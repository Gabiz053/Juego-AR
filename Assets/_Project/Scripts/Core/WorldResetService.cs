// ------------------------------------------------------------
//  WorldResetService.cs  -  _Project.Scripts.Core
//  Encapsulates the "clear all blocks" world-reset operation.
// ------------------------------------------------------------

using System;
using UnityEngine;
using _Project.Scripts.AR;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Destroys every placed block, resets the AR spatial anchor, and
    /// deactivates the construction grid.  Reusable independently of UI.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/World Reset Service")]
    public class WorldResetService : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("AR World References")]
        [Tooltip("Transform that parents all placed blocks.")]
        [SerializeField] private Transform _worldContainer;

        [Tooltip("ARWorldManager -- resets the AR spatial anchor.")]
        [SerializeField] private ARWorldManager _arWorldManager;

        [Tooltip("GridManager -- deactivates the grid after clearing.")]
        [SerializeField] private GridManager _gridManager;

        [Tooltip("UndoRedoService -- stacks are cleared on reset.")]
        [SerializeField] private UndoRedoService _undoRedoService;

        [Header("Harmony")]
        [Tooltip("HarmonyService -- reset to zero when cleared.")]
        [SerializeField] private HarmonyService _harmonyService;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised after the world has been fully reset.</summary>
        public event Action OnWorldReset;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>Current number of placed blocks under the world container.</summary>
        public int BlockCount => _worldContainer != null ? _worldContainer.childCount : 0;

        /// <summary>
        /// Full reset: destroy blocks, reset anchor, hide grid, clear stacks.
        /// </summary>
        public void ResetWorld()
        {
            int count = BlockCount;
            DestroyAllBlocks();
            ResetAnchor();
            DeactivateGrid();
            _undoRedoService?.Clear();
            _harmonyService?.NotifyWorldReset();
            OnWorldReset?.Invoke();
            Debug.Log($"[WorldResetService] World reset complete -- destroyed {count} objects.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Iterates WorldContainer children in reverse, destroying only
        /// GameObjects that carry <see cref="VoxelBlock"/> or
        /// <see cref="ProceduralPebble"/> — leaving the grid visual intact.
        /// </summary>
        private void DestroyAllBlocks()
        {
            if (_worldContainer == null) return;

            for (int i = _worldContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = _worldContainer.GetChild(i).gameObject;

                bool isBlock  = child.GetComponent<VoxelBlock>()       != null;
                bool isPebble = child.GetComponent<ProceduralPebble>() != null;

                if (isBlock || isPebble)
                    Destroy(child);
            }
        }

        /// <summary>Calls <see cref="ARWorldManager.ResetAnchor"/> to unpin the world.</summary>
        private void ResetAnchor()
        {
            if (_arWorldManager != null)
                _arWorldManager.ResetAnchor();
        }

        /// <summary>Hides the construction grid via <see cref="GridManager.DeactivateGrid"/>.</summary>
        private void DeactivateGrid()
        {
            if (_gridManager != null)
                _gridManager.DeactivateGrid();
        }

        #endregion

        #region Validation ----------------------------------------

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
            if (_harmonyService == null)
                Debug.LogError("[WorldResetService] _harmonyService is not assigned!", this);
        }

        #endregion
    }
}
