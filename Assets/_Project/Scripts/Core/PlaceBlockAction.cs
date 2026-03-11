// ------------------------------------------------------------
//  PlaceBlockAction.cs  -  _Project.Scripts.Core
//  Undoable command for placing a single voxel block.
// ------------------------------------------------------------

using UnityEngine;
using _Project.Scripts.Voxel;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Records a block placement so it can be undone (destroy the block)
    /// or redone (re-instantiate it at the same position).<br/>
    /// Created by <see cref="Interaction.ARBlockPlacer"/> after every
    /// successful placement and pushed to <see cref="UndoRedoService"/>.
    /// </summary>
    public sealed class PlaceBlockAction : IUndoableAction
    {
        #region State -------------------------------------------------

        private GameObject _instance;

        private readonly GameObject _prefab;
        private readonly Transform  _parent;
        private readonly Vector3    _localPosition;
        private readonly Quaternion _localRotation;

        #endregion

        #region Constructor -------------------------------------------

        public PlaceBlockAction(
            GameObject instance,
            GameObject prefab,
            Transform  parent,
            Vector3    localPosition,
            Quaternion localRotation)
        {
            _instance      = instance;
            _prefab        = prefab;
            _parent        = parent;
            _localPosition = localPosition;
            _localRotation = localRotation;
        }

        #endregion

        #region IUndoableAction ---------------------------------------

        /// <summary>Destroy the placed block without physics tumble.</summary>
        public void Undo()
        {
            if (_instance == null) return;

            Object.Destroy(_instance);
            _instance = null;

            Debug.Log($"[PlaceBlockAction] Undo — destroyed block at {_localPosition}.");
        }

        /// <summary>Re-instantiate the block at its snapped grid position.</summary>
        public void Redo()
        {
            if (_prefab == null || _parent == null) return;

            _instance = Object.Instantiate(_prefab, _parent);
            _instance.transform.SetLocalPositionAndRotation(_localPosition, _localRotation);

            ArmForImmediate(_instance);

            Debug.Log($"[PlaceBlockAction] Redo — restored {_prefab.name} at {_localPosition}.");
        }

        #endregion

        #region Shared ------------------------------------------------

        /// <summary>
        /// Prepares a freshly-instantiated block for immediate use:
        /// disables fly-in animation, enables collider, arms proximity
        /// detection.  Called by both <see cref="Redo"/> and
        /// <see cref="DestroyBlockAction.Undo"/>.
        /// </summary>
        internal static void ArmForImmediate(GameObject instance)
        {
            BlockSpawn bs = instance.GetComponent<BlockSpawn>();
            if (bs != null) bs.enabled = false;

            Collider col = instance.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            BlockDestroy bd = instance.GetComponent<BlockDestroy>();
            if (bd != null)
            {
                bd.enabled = true;
                bd.SetReady();
            }
        }

        #endregion
    }
}
