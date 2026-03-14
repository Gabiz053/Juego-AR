// ------------------------------------------------------------
//  UndoRedoService.cs  -  _Project.Scripts.Core
//  Command-pattern undo / redo stack.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Infrastructure;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Maintains an undo stack and a redo stack of <see cref="IUndoableAction"/>
    /// entries.  Other systems push actions via <see cref="Record"/> after
    /// executing them.  <see cref="UI.UndoRedoHUD"/> subscribes to
    /// <see cref="OnStackChanged"/> to keep buttons in sync.<br/>
    /// Publishes <see cref="UndoPerformedEvent"/> / <see cref="RedoPerformedEvent"/>
    /// via <see cref="EventBus"/> so dependent systems (e.g. HarmonyService)
    /// can react without a direct reference.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Core/Undo Redo Service")]
    public class UndoRedoService : MonoBehaviour, IUndoRedoService
    {
        #region Inspector -----------------------------------------

        [Header("History")]
        [Tooltip("Maximum actions kept in the undo stack. Oldest entries are discarded.")]
        [SerializeField] private int _maxHistory = 20;

        #endregion

        #region Events --------------------------------------------

        /// <summary>
        /// Raised after every <see cref="Record"/>, <see cref="Undo"/> or
        /// <see cref="Redo"/>.  Carries (canUndo, canRedo).
        /// </summary>
        public event Action<bool, bool> OnStackChanged;

        #endregion

        #region State ---------------------------------------------

        private readonly Stack<IUndoableAction> _undoStack = new Stack<IUndoableAction>();
        private readonly Stack<IUndoableAction> _redoStack = new Stack<IUndoableAction>();

        #endregion

        #region Public API ----------------------------------------

        /// <summary>True when at least one action can be undone.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>True when at least one action can be redone.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Records a completed action.  Clears the redo stack (no branching).
        /// </summary>
        public void Record(IUndoableAction action)
        {
            if (action == null) return;

            if (_undoStack.Count >= _maxHistory)
                TrimBottom(_undoStack);

            _undoStack.Push(action);
            _redoStack.Clear();
            NotifyChanged();
            Debug.Log($"[UndoRedoService] Recorded {action.GetType().Name} -- undo: {_undoStack.Count}, redo: {_redoStack.Count}.");
        }

        /// <summary>Undoes the most recent action.</summary>
        public void Undo()
        {
            if (!CanUndo) return;

            IUndoableAction action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);

            NotifyChanged();
            EventBus.Publish(new UndoPerformedEvent());
            Debug.Log($"[UndoRedoService] Undo {action.GetType().Name} -- undo: {_undoStack.Count}, redo: {_redoStack.Count}.");
        }

        /// <summary>Re-applies the most recently undone action.</summary>
        public void Redo()
        {
            if (!CanRedo) return;

            IUndoableAction action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);

            NotifyChanged();
            EventBus.Publish(new RedoPerformedEvent());
            Debug.Log($"[UndoRedoService] Redo {action.GetType().Name} -- undo: {_undoStack.Count}, redo: {_redoStack.Count}.");
        }

        /// <summary>Clears both stacks (called on world reset).</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            NotifyChanged();
            Debug.Log("[UndoRedoService] Stacks cleared.");
        }

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            ServiceLocator.Register<IUndoRedoService>(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<WorldResetEvent>(HandleWorldReset);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WorldResetEvent>(HandleWorldReset);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IUndoRedoService>();
        }

        #endregion

        #region EventBus Handlers ---------------------------------

        /// <summary>Auto-clears stacks when the world is reset.</summary>
        private void HandleWorldReset(WorldResetEvent _)
        {
            Clear();
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>Fires the <see cref="OnStackChanged"/> event with current state.</summary>
        private void NotifyChanged() =>
            OnStackChanged?.Invoke(CanUndo, CanRedo);

        /// <summary>
        /// Removes the oldest element from a stack.  O(n) copy but only
        /// runs when the history cap is hit, which is rare.
        /// </summary>
        private static void TrimBottom<T>(Stack<T> stack)
        {
            var tmp = new List<T>(stack);
            stack.Clear();
            for (int i = tmp.Count - 2; i >= 0; i--)
                stack.Push(tmp[i]);
        }

        #endregion
    }
}
