// ------------------------------------------------------------
//  ToolManager.cs  -  _Project.Scripts.Interaction
//  Manages the selected tool / block and provides the matching prefab.
// ------------------------------------------------------------

using System;
using UnityEngine;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Tracks which tool the player has selected and provides the
    /// corresponding block prefab via <see cref="BlockDatabase"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Tool Manager")]
    public class ToolManager : MonoBehaviour
    {
        #region Constants -----------------------------------------

        private const ToolType DEFAULT_TOOL     = ToolType.Build_Sand;
        private const ToolType FIRST_BUILD_TOOL = ToolType.Build_Sand;
        private const ToolType LAST_BUILD_TOOL  = ToolType.Build_Grass;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Block Data")]
        [Tooltip("ScriptableObject mapping each BlockType to its prefab.")]
        [SerializeField] private BlockDatabase _blockDatabase;

        #endregion

        #region Events --------------------------------------------

        /// <summary>Raised whenever the selected tool changes.</summary>
        public event Action<ToolType> OnToolChanged;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>The tool currently selected by the player.</summary>
        public ToolType CurrentTool { get; private set; } = DEFAULT_TOOL;

        /// <summary><c>true</c> when the current tool is a block-building tool (0-5).</summary>
        public bool IsBuildTool => CurrentTool >= FIRST_BUILD_TOOL
                                && CurrentTool <= LAST_BUILD_TOOL;

        /// <summary>
        /// Selects a tool by its integer index (matches <see cref="ToolType"/> values).
        /// </summary>
        public void SelectToolByIndex(int index)
        {
            if (!Enum.IsDefined(typeof(ToolType), index))
            {
                Debug.LogWarning($"[ToolManager] Invalid tool index {index}.");
                return;
            }

            CurrentTool = (ToolType)index;
            OnToolChanged?.Invoke(CurrentTool);
            Debug.Log($"[ToolManager] Tool changed to {CurrentTool}.");
        }

        /// <summary>Returns the prefab for the current build tool, or null.</summary>
        public GameObject GetCurrentBlockPrefab()
        {
            if (!IsBuildTool) return null;

            BlockType blockType = (BlockType)(int)CurrentTool;

            if (_blockDatabase == null)
            {
                Debug.LogError("[ToolManager] _blockDatabase is not assigned!", this);
                return null;
            }

            return _blockDatabase.GetPrefab(blockType);
        }

        /// <summary>Returns the prefab for a specific <see cref="BlockType"/>.</summary>
        public GameObject GetBlockPrefab(BlockType blockType)
        {
            if (_blockDatabase == null)
            {
                Debug.LogError("[ToolManager] _blockDatabase is not assigned!", this);
                return null;
            }
            return _blockDatabase.GetPrefab(blockType);
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (_blockDatabase == null)
                Debug.LogError("[ToolManager] _blockDatabase is not assigned!", this);
        }

        #endregion
    }
}
