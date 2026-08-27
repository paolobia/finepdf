namespace FinePdf.Core.Operations;

/// <inheritdoc />
public sealed class UndoRedoStack : IUndoRedoStack
{
    private readonly Stack<IUndoableAction> _undo = new();
    private readonly Stack<IUndoableAction> _redo = new();

    public event EventHandler? Changed;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public string? NextUndoDescription => CanUndo ? _undo.Peek().Description : null;

    public string? NextRedoDescription => CanRedo ? _redo.Peek().Description : null;

    public void Execute(IUndoableAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        action.Do();
        _undo.Push(action);
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        IUndoableAction action = _undo.Pop();
        action.Undo();
        _redo.Push(action);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        IUndoableAction action = _redo.Pop();
        action.Do();
        _undo.Push(action);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_undo.Count == 0 && _redo.Count == 0)
        {
            return;
        }

        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
