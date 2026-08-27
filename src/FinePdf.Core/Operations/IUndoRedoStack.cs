namespace FinePdf.Core.Operations;

/// <summary>Executes <see cref="IUndoableAction"/>s and keeps the undo/redo history.</summary>
public interface IUndoRedoStack
{
    bool CanUndo { get; }

    bool CanRedo { get; }

    string? NextUndoDescription { get; }

    string? NextRedoDescription { get; }

    /// <summary>Raised after any change to the stack (execute, undo, redo, clear).</summary>
    event EventHandler? Changed;

    /// <summary>Runs <see cref="IUndoableAction.Do"/>, pushes it onto the undo stack and clears the redo stack.</summary>
    void Execute(IUndoableAction action);

    void Undo();

    void Redo();

    void Clear();
}
