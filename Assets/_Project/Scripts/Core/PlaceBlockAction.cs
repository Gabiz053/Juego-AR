// ------------------------------------------------------------
//  PlaceBlockAction.cs  -  _Project.Scripts.Core
//  Undoable command for placing a single voxel block.
// ------------------------------------------------------------

using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Records a block placement so it can be undone (destroy the block)
    /// or redone (re-instantiate it at the same position).<br/>
    /// Created and pushed to <see cref="UndoRedoService"/> by
    /// <see cref="Interaction.ARBlockPlacer"/> after every successful placement.
    /// </summary>
    public sealed class PlaceBlockAction : IUndoableAction
    {
        #region State -------------------------------------------------

        private GameObject _instance;

        private readonly GameObject         _prefab;
        private readonly Transform          _parent;
        private readonly Vector3            _localPosition;
        private readonly Quaternion         _localRotation;
        private readonly GameObject         _breakVfxPrefab;
        private readonly GameAudioService   _audioService;
        private readonly Action<GameObject> _onInstantiated;

        #endregion

        #region Constructor -------------------------------------------

        public PlaceBlockAction(
            GameObject         instance,
            GameObject         prefab,
            Transform          parent,
            Vector3            localPosition,
            Quaternion         localRotation,
            GameObject         breakVfxPrefab,
            GameAudioService   audioService,
            Action<GameObject> onInstantiated)
        {
            _instance       = instance;
            _prefab         = prefab;
            _parent         = parent;
            _localPosition  = localPosition;
            _localRotation  = localRotation;
            _breakVfxPrefab = breakVfxPrefab;
            _audioService   = audioService;
            _onInstantiated = onInstantiated;
        }

        #endregion

        #region IUndoableAction ---------------------------------------

        /// <summary>Destroy the placed block without physics tumble.</summary>
        public void Undo()
        {
            if (_instance == null) return;

            UnityEngine.Object.Destroy(_instance);
            _instance = null;
        }

        /// <summary>Re-instantiate the block at its snapped grid position.</summary>
        public void Redo()
        {
            if (_prefab == null || _parent == null) return;

            _instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            _instance.transform.SetLocalPositionAndRotation(_localPosition, Quaternion.identity);

            _onInstantiated?.Invoke(_instance);
        }

        #endregion
    }
}
