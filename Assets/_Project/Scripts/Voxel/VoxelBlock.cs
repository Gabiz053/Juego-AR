// ------------------------------------------------------------
//  VoxelBlock.cs  -  _Project.Scripts.Voxel
//  Per-instance data attached to every placed block prefab.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Holds the runtime data for a single voxel block instance.<br/>
    /// Attach to every block prefab so the game can identify its type
    /// and play the correct audio feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Voxel Block")]
    public class VoxelBlock : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Identity")]
        [Tooltip("Block type this prefab represents.")]
        [SerializeField] private BlockType _blockType;

        [Header("Audio Feedback")]
        [Tooltip("Clips played at random when this block is placed.")]
        [SerializeField] private AudioClip[] _placeSounds;

        [Tooltip("Clips played at random when this block is destroyed.")]
        [SerializeField] private AudioClip[] _breakSounds;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>The block type this instance belongs to.</summary>
        public BlockType Type => _blockType;

        /// <summary>Pool of clips to pick from on placement.</summary>
        public AudioClip[] PlaceSounds => _placeSounds;

        /// <summary>Pool of clips to pick from on destruction.</summary>
        public AudioClip[] BreakSounds => _breakSounds;

        #endregion
    }
}
