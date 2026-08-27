namespace FinePdf.Core.Operations;

/// <summary>
/// Groups several actions so they undo/redo as one step (e.g. rotating every selected sheet).
/// Undo runs the children in reverse order.
/// </summary>
public sealed class CompositeAction : IUndoableAction
{
    private readonly IReadOnlyList<IUndoableAction> _actions;

    public CompositeAction(string description, IEnumerable<IUndoableAction> actions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(actions);

        _actions = [.. actions];
        if (_actions.Count == 0)
        {
            throw new ArgumentException("A composite action needs at least one child action.", nameof(actions));
        }

        Description = description;
    }

    public string Description { get; }

    public void Do()
    {
        foreach (IUndoableAction action in _actions)
        {
            action.Do();
        }
    }

    public void Undo()
    {
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo();
        }
    }
}
