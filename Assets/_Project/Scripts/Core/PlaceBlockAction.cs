// ??????????????????????????????????????????????
//  PlaceBlockAction.cs  ·  _Project.Scripts.Core
//  Undoable command for placing a single voxel block.
// ??????????????????????????????????????????????

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
        // The live instance — null after Undo, valid after Redo.
        private GameObject _instance;

        // Everything needed to recreate the block on Redo.
        private readonly GameObject  _prefab;
        private readonly Transform   _parent;
        private readonly Vector3     _localPosition;
        private readonly Quaternion  _localRotation;

        // Shared refs forwarded to BlockDestroy after each Redo instantiation.
        private readonly GameObject              _breakVfxPrefab;
        private readonly GameAudioService        _audioService;

        // Callback that arms BlockDestroy once the block is ready
        // (mirrors the BlockSpawn.Play callback used during normal placement).
        private readonly System.Action<GameObject> _onInstantiated;

        public PlaceBlockAction(
            GameObject          instance,
            GameObject          prefab,
            Transform           parent,
            Vector3             localPosition,
            Quaternion          localRotation,
            GameObject          breakVfxPrefab,
            GameAudioService    audioService,
            System.Action<GameObject> onInstantiated)
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

        /// <summary>Undo: immediately destroy the placed block without physics tumble.</summary>
        public void Undo()
        {
            if (_instance != null)
            {
                Object.Destroy(_instance);
                _instance = null;
            }
        }

        /// <summary>Redo: re-instantiate the block at the snapped grid position.</summary>
        public void Redo()
        {
            if (_prefab == null || _parent == null) return;

            _instance = Object.Instantiate(_prefab, _parent);
            // Always use identity rotation — blocks are always axis-aligned.
            _instance.transform.SetLocalPositionAndRotation(_localPosition, Quaternion.identity);

            _onInstantiated?.Invoke(_instance);
        }
    }
}
