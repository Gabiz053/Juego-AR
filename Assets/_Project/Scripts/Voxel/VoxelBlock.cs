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
    /// identify its type and play the correct audio feedback.<br/>
    /// Each sound slot accepts multiple clips — one is chosen at random
    /// on every play call (handled by <see cref="Core.GameAudioService"/>).
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
        [Tooltip("One or more clips played at random when this block is placed in the world.")]
        [SerializeField] private AudioClip[] _placeSounds;

        [Tooltip("One or more clips played at random when this block is destroyed.")]
        [SerializeField] private AudioClip[] _breakSounds;

        #endregion

        #region Public API ────────────────────────────────────

        /// <summary>The block type this instance belongs to.</summary>
        public BlockType Type => _blockType;

        /// <summary>
        /// Pool of clips to pick from on placement.
        /// Can be null or empty — <see cref="Core.GameAudioService"/> handles both silently.
        /// </summary>
        public AudioClip[] PlaceSounds => _placeSounds;

        /// <summary>
        /// Pool of clips to pick from on destruction.
        /// Can be null or empty — <see cref="Core.GameAudioService"/> handles both silently.
        /// </summary>
        public AudioClip[] BreakSounds => _breakSounds;

        #endregion
    }
}
