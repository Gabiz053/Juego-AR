// ------------------------------------------------------------
//  BlockDatabaseSO.cs  -  _Project.Scripts.Core
//  Central registry that maps every BlockType to its prefab.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// ScriptableObject asset that holds the complete catalogue of block
    /// prefabs keyed by <see cref="BlockType"/>.<br/>
    /// Create one via <c>Assets > Create > ARmonia > Voxel > Block Database</c>
    /// and reference it from any system that needs to spawn or query blocks.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BlockDatabase",
        menuName = "ARmonia/Voxel/Block Database",
        order    = 0)]
    public class BlockDatabaseSO : ScriptableObject
    {
        #region Nested Types --------------------------------------

        /// <summary>
        /// A single entry pairing a <see cref="BlockType"/> with its prefab.
        /// </summary>
        [Serializable]
        public struct BlockEntry
        {
            [Tooltip("Block type this entry defines.")]
            public BlockType type;

            [Tooltip("Prefab to instantiate when placing this block type.")]
            public GameObject prefab;
        }

        #endregion

        #region Inspector -----------------------------------------

        [Header("Block Catalogue")]
        [Tooltip("One entry per BlockType. Order does not matter -- lookup is by type.")]
        [SerializeField] private BlockEntry[] _entries = Array.Empty<BlockEntry>();

        #endregion

        #region Runtime Lookup ------------------------------------

        private Dictionary<BlockType, GameObject> _lookup;

        /// <summary>Total number of registered block entries.</summary>
        public int Count => _entries.Length;

        /// <summary>
        /// Returns the prefab for <paramref name="type"/>, or <c>null</c>
        /// if no entry exists.
        /// </summary>
        public GameObject GetPrefab(BlockType type)
        {
            EnsureLookup();
            return _lookup.TryGetValue(type, out GameObject prefab) ? prefab : null;
        }

        /// <summary>
        /// Tries to retrieve the prefab for <paramref name="type"/>.
        /// Returns <c>true</c> when found.
        /// </summary>
        public bool TryGetPrefab(BlockType type, out GameObject prefab)
        {
            EnsureLookup();
            return _lookup.TryGetValue(type, out prefab);
        }

        #endregion

        #region Internals -----------------------------------------

        private void EnsureLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<BlockType, GameObject>(_entries.Length);
            foreach (BlockEntry entry in _entries)
            {
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"[BlockDatabaseSO] Entry for {entry.type} has no prefab.", this);
                    continue;
                }
                _lookup[entry.type] = entry.prefab;
            }
        }

        /// <summary>
        /// Called by Unity on asset load and after every domain reload.
        /// Forces the dictionary to rebuild next time it is queried.
        /// </summary>
        private void OnEnable() => _lookup = null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<BlockType>();
            foreach (BlockEntry entry in _entries)
            {
                if (!seen.Add(entry.type))
                    Debug.LogWarning($"[BlockDatabaseSO] Duplicate entry for {entry.type}.", this);
            }
        }
#endif

        #endregion
    }
}
