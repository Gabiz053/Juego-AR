// ??????????????????????????????????????????????
//  DestroyBlockAction.cs  ·  _Project.Scripts.Core
//  Undoable command for destroying a single voxel block.
// ??????????????????????????????????????????????

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Records a block destruction so it can be undone (restore the block)
    /// or redone (destroy it again).<br/>
    /// Created and pushed to <see cref="UndoRedoService"/> by
    /// <see cref="Interaction.ARBlockPlacer"/> just before <see cref="Voxel.BlockDestroy"/>
    /// is triggered.
    /// </summary>
    public sealed class DestroyBlockAction : IUndoableAction
    {
        // Everything needed to restore the block on Undo.
        private readonly GameObject           _prefab;
        private readonly Transform            _parent;
        private readonly Vector3              _localPosition;
        private readonly Quaternion           _localRotation;
        private readonly GameObject           _breakVfxPrefab;
        private readonly GameAudioService     _audioService;
        private readonly System.Action<GameObject> _onInstantiated;

        // The restored instance — valid after Undo, null after Redo.
        private GameObject _restoredInstance;

        public DestroyBlockAction(
            GameObject          prefab,
            Transform           parent,
            Vector3             localPosition,
            Quaternion          localRotation,
            GameObject          breakVfxPrefab,
            GameAudioService    audioService,
            System.Action<GameObject> onInstantiated)
        {
            _prefab         = prefab;
            _parent         = parent;
            _localPosition  = localPosition;
            _localRotation  = localRotation;
            _breakVfxPrefab = breakVfxPrefab;
            _audioService   = audioService;
            _onInstantiated = onInstantiated;
        }

        /// <summary>Undo: restore the block at its original snapped grid position.</summary>
        public void Undo()
        {
            if (_prefab == null || _parent == null) return;

            _restoredInstance = Object.Instantiate(_prefab, _parent);
            // Always use identity rotation — blocks are always axis-aligned.
            _restoredInstance.transform.SetLocalPositionAndRotation(_localPosition, Quaternion.identity);

            _onInstantiated?.Invoke(_restoredInstance);
        }

        /// <summary>Redo: destroy the restored block immediately without physics tumble.</summary>
        public void Redo()
        {
            if (_restoredInstance != null)
            {
                Object.Destroy(_restoredInstance);
                _restoredInstance = null;
            }
        }
    }
}
