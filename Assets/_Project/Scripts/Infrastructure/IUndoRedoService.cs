// ------------------------------------------------------------
//  IUndoRedoService.cs  -  _Project.Scripts.Infrastructure
//  Contract for the command-pattern undo / redo stack.
// ------------------------------------------------------------

using System;

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Records <see cref="IUndoableAction"/> entries and provides
    /// undo / redo traversal.  Fires <see cref="OnStackChanged"/>
    /// so UI can reflect button availability.
    /// </summary>
    public interface IUndoRedoService
    {
        /// <summary>True when at least one action can be undone.</summary>
        bool CanUndo { get; }

        /// <summary>True when at least one action can be redone.</summary>
        bool CanRedo { get; }

        /// <summary>Records a completed action. Clears the redo stack.</summary>
        void Record(IUndoableAction action);

        /// <summary>Undoes the most recent action.</summary>
        void Undo();

        /// <summary>Re-applies the most recently undone action.</summary>
        void Redo();

        /// <summary>Clears both stacks.</summary>
        void Clear();

        /// <summary>Raised after every Record, Undo, or Redo. Carries (canUndo, canRedo).</summary>
        event Action<bool, bool> OnStackChanged;
    }
}
