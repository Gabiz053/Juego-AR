// ──────────────────────────────────────────────
//  ToolManager.cs  ·  _Project.Scripts.Interaction
//  Manages the selected tool/block and provides the matching prefab.
// ──────────────────────────────────────────────

using System;
using UnityEngine;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Tracks which tool the player has selected and provides the
    /// corresponding block prefab via <see cref="BlockDatabase"/>.<br/>
    /// Other systems subscribe to <see cref="OnToolChanged"/> to react
    /// when the selection changes (UI highlight, orientation manager, etc.).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Interaction/Tool Manager")]
    public class ToolManager : MonoBehaviour
    {
        #region Constants ─────────────────────────────────────

        /// <summary>Tool selected at startup before the player touches anything.</summary>
        private const ToolType DEFAULT_TOOL = ToolType.Build_Sand;

        /// <summary>First enum value that counts as a build tool.</summary>
        private const ToolType FIRST_BUILD_TOOL = ToolType.Build_Sand;

        /// <summary>Last enum value that counts as a build tool.</summary>
        private const ToolType LAST_BUILD_TOOL = ToolType.Build_Grass;

        #endregion

        #region Inspector ─────────────────────────────────────

        [Header("Block Data")]
        [Tooltip("ScriptableObject mapping each BlockType to its prefab. Create via Assets → Create → ARmonia → Voxel → Block Database.")]
        [SerializeField] private BlockDatabase _blockDatabase;

        #endregion

        #region Events ────────────────────────────────────────

        /// <summary>
        /// Raised whenever the selected tool changes.
        /// Subscribers receive the new <see cref="ToolType"/>.
        /// </summary>
        public event Action<ToolType> OnToolChanged;

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>The tool currently selected by the player.</summary>
        public ToolType CurrentTool { get; private set; } = DEFAULT_TOOL;

        /// <summary>
        /// <c>true</c> when the current tool is a block-building tool
        /// (<see cref="ToolType.Build_Dirt"/> through <see cref="ToolType.Build_Torch"/>).
        /// </summary>
        public bool IsBuildTool => CurrentTool >= FIRST_BUILD_TOOL
                                && CurrentTool <= LAST_BUILD_TOOL;

        /// <summary>
        /// Selects a tool by its integer index (matching <see cref="ToolType"/>
        /// values 0–8).  Called indirectly by UI buttons via
        /// <see cref="UI.UIManager.OnSlotClicked"/>.
        /// </summary>
        /// <param name="index">Integer matching a <see cref="ToolType"/> value.</param>
        public void SelectToolByIndex(int index)
        {
            if (!Enum.IsDefined(typeof(ToolType), index))
            {
                Debug.LogWarning($"[ToolManager] Invalid tool index {index} — ignoring.");
                return;
            }

            ToolType newTool = (ToolType)index;
            CurrentTool = newTool;
            OnToolChanged?.Invoke(CurrentTool);

            Debug.Log($"[ToolManager] Tool selected: {CurrentTool} (index {index}).");
        }

        /// <summary>
        /// Returns the prefab for the currently selected build tool,
        /// or <c>null</c> if no building tool is active or the database
        /// has no matching entry.
        /// </summary>
        public GameObject GetCurrentBlockPrefab()
        {
            if (!IsBuildTool) return null;

            // Build_Sand(0)→BlockType.Sand(0), Build_Glass(1)→BlockType.Glass(1), etc.
            BlockType blockType = (BlockType)(int)CurrentTool;

            if (_blockDatabase == null)
            {
                Debug.LogError("[ToolManager] _blockDatabase is not assigned!", this);
                return null;
            }

            GameObject prefab = _blockDatabase.GetPrefab(blockType);

            if (prefab == null)
                Debug.LogWarning($"[ToolManager] No prefab found in BlockDatabase for {blockType}.", this);

            return prefab;
        }

        #endregion

        #region Unity Lifecycle ────────────────────────────────

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"[ToolManager] Initialized — default tool: {CurrentTool}.");
        }

        #endregion

        #region Validation ────────────────────────────────────

        /// <summary>
        /// Logs errors for any missing Inspector references at startup.
        /// </summary>
        private void ValidateReferences()
        {
            if (_blockDatabase == null)
                Debug.LogError("[ToolManager] _blockDatabase is not assigned! Block placement will fail.", this);
        }

        #endregion
    }
}
