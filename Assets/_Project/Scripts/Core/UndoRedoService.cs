// ??????????????????????????????????????????????
//  UndoRedoService.cs  ·  _Project.Scripts.Core
//  Command-pattern undo/redo stack.
//  Records IUndoableActions and replays them on demand.
// ??????????????????????????????????????????????

using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Maintains an undo stack and a redo stack of <see cref="IUndoableAction"/> entries.<br/>
    /// Other systems push actions via <see cref="Record"/> after executing them.<br/>
    /// <see cref="UndoRedoHUD"/> subscribes to <see cref="OnStackChanged"/> to keep
    /// the buttons visually in sync.<br/>
    /// Attach to any persistent GameObject (e.g. <c>XR Origin</c>).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Undo Redo Service")]
    public class UndoRedoService : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Tooltip("Maximum number of actions kept in the undo stack. Oldest entries are discarded.")]
        [SerializeField] private int _maxHistory = 20;

        [Header("Harmony")]
        [Tooltip("HarmonyService — rescans garden after every undo/redo.")]
        [SerializeField] private HarmonyService _harmonyService;

        #endregion

        #region Events ????????????????????????????????????????

        /// <summary>
        /// Raised after every <see cref="Record"/>, <see cref="Undo"/> or <see cref="Redo"/>.<br/>
        /// Carries (canUndo, canRedo) so the HUD can refresh in a single call.
        /// </summary>
        public event Action<bool, bool> OnStackChanged;

        #endregion

        #region State ?????????????????????????????????????????

        private readonly Stack<IUndoableAction> _undoStack = new Stack<IUndoableAction>();
        private readonly Stack<IUndoableAction> _redoStack = new Stack<IUndoableAction>();

        #endregion

        #region Public API ????????????????????????????????????

        /// <summary>True when there is at least one action that can be undone.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>True when there is at least one action that can be redone.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Records a completed action onto the undo stack.<br/>
        /// Clears the redo stack — branching history is not supported.<br/>
        /// If the stack exceeds <see cref="_maxHistory"/> the oldest entry is dropped.
        /// </summary>
        public void Record(IUndoableAction action)
        {
            if (action == null) return;

            // Trim oldest entry if we hit the cap.
            if (_undoStack.Count >= _maxHistory)
                TrimBottom(_undoStack);

            _undoStack.Push(action);

            // Any new action invalidates the redo branch.
            _redoStack.Clear();

            NotifyChanged();
            Debug.Log($"[UndoRedoService] Recorded {action.GetType().Name}. " +
                      $"Undo={_undoStack.Count} Redo={_redoStack.Count}");
        }

        /// <summary>
        /// Undoes the most recent action and pushes it onto the redo stack.
        /// No-op if the undo stack is empty.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            IUndoableAction action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);

            NotifyChanged();
            _harmonyService?.NotifyUndoRedo();
            Debug.Log($"[UndoRedoService] Undid {action.GetType().Name}. " +
                      $"Undo={_undoStack.Count} Redo={_redoStack.Count}");
        }

        /// <summary>
        /// Re-applies the most recently undone action and pushes it back onto
        /// the undo stack. No-op if the redo stack is empty.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            IUndoableAction action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);

            NotifyChanged();
            _harmonyService?.NotifyUndoRedo();
            Debug.Log($"[UndoRedoService] Redid {action.GetType().Name}. " +
                      $"Undo={_undoStack.Count} Redo={_redoStack.Count}");
        }

        /// <summary>
        /// Clears both stacks — called when the world is fully reset.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            NotifyChanged();
            Debug.Log("[UndoRedoService] Stacks cleared.");
        }

        #endregion

        #region Helpers ???????????????????????????????????????

        private void NotifyChanged() =>
            OnStackChanged?.Invoke(CanUndo, CanRedo);

        /// <summary>
        /// Removes the bottom (oldest) element of a stack by temporarily
        /// reversing it into a list. O(n) but only runs when the cap is hit.
        /// </summary>
        private static void TrimBottom<T>(Stack<T> stack)
        {
            var tmp = new List<T>(stack);
            stack.Clear();
            // tmp[0] is the top (newest) — skip tmp[last] (oldest).
            for (int i = tmp.Count - 2; i >= 0; i--)
                stack.Push(tmp[i]);
        }

        #endregion
    }
}
