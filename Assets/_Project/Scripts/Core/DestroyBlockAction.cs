// ------------------------------------------------------------
//  DestroyBlockAction.cs  -  _Project.Scripts.Core
//  Undoable command for destroying a single voxel block.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Records a block destruction so it can be undone (restore the block)
    /// or redone (destroy it again).<br/>
    /// Created by <see cref="Interaction.BlockDestroyer"/> (tool tap) and
    /// <see cref="Voxel.BlockDestroy"/> (proximity knock) just before the
    /// physics-tumble sequence begins.
    /// </summary>
    public sealed class DestroyBlockAction : IUndoableAction
    {
        #region State -------------------------------------------------

        private GameObject _restoredInstance;

        private readonly GameObject _prefab;
        private readonly Transform  _parent;
        private readonly Vector3    _localPosition;
        private readonly Quaternion _localRotation;

        #endregion

        #region Constructor -------------------------------------------

        public DestroyBlockAction(
            GameObject prefab,
            Transform  parent,
            Vector3    localPosition,
            Quaternion localRotation)
        {
            _prefab        = prefab;
            _parent        = parent;
            _localPosition = localPosition;
            _localRotation = localRotation;
        }

        #endregion

        #region IUndoableAction ---------------------------------------

        /// <summary>Restore the block at its original snapped grid position.</summary>
        public void Undo()
        {
            if (_prefab == null || _parent == null) return;

            _restoredInstance = Object.Instantiate(_prefab, _parent);
            _restoredInstance.transform.SetLocalPositionAndRotation(_localPosition, _localRotation);

            PlaceBlockAction.ArmForImmediate(_restoredInstance);

            Debug.Log($"[DestroyBlockAction] Undo — restored {_prefab.name} at {_localPosition}.");
        }

        /// <summary>Destroy the restored block immediately without physics tumble.</summary>
        public void Redo()
        {
            if (_restoredInstance == null) return;

            Object.Destroy(_restoredInstance);
            _restoredInstance = null;

            Debug.Log($"[DestroyBlockAction] Redo — destroyed block at {_localPosition}.");
        }

        #endregion
    }
}
