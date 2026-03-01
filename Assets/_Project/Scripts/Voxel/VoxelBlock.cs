// ──────────────────────────────────────────────
//  VoxelBlock.cs  ·  _Project.Scripts.Voxel
//  Per-instance data attached to every placed block prefab.
// ──────────────────────────────────────────────

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Holds the runtime data for a single voxel block instance.<br/>
    /// Attach this component to every block prefab so the game can
    /// identify its type and play the correct audio feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Voxel Block")]
    public class VoxelBlock : MonoBehaviour
    {
        #region Inspector ─────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Block type this prefab represents (Dirt, Sand, Stone, Wood, Torch).")]
        [SerializeField] private BlockType _blockType;

        [Header("Audio Feedback")]
        [Tooltip("Sound played when this block is placed in the world.")]
        [SerializeField] private AudioClip _placeSound;

        [Tooltip("Sound played when this block is destroyed.")]
        [SerializeField] private AudioClip _breakSound;

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>The block type this instance belongs to.</summary>
        public BlockType Type => _blockType;

        /// <summary>Audio clip to play on placement. Can be <c>null</c>.</summary>
        public AudioClip PlaceSound => _placeSound;

        /// <summary>Audio clip to play on destruction. Can be <c>null</c>.</summary>
        public AudioClip BreakSound => _breakSound;

        #endregion
    }
}
