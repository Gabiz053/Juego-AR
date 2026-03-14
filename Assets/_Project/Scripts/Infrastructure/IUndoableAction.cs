// ------------------------------------------------------------
//  IUndoableAction.cs  -  _Project.Scripts.Infrastructure
//  Contract every undoable / redoable action must implement.
// ------------------------------------------------------------

namespace _Project.Scripts.Infrastructure
{
    /// <summary>
    /// Contract for a reversible action in the Command pattern.<br/>
    /// <see cref="IUndoRedoService"/> stores a stack of these and calls
    /// <see cref="Undo"/> / <see cref="Redo"/> on demand.
    /// </summary>
    public interface IUndoableAction
    {
        /// <summary>Reverses the action.</summary>
        void Undo();

        /// <summary>Re-applies the action after it has been undone.</summary>
        void Redo();
    }
}
