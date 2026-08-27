namespace FinePdf.Core.Operations;

/// <summary>
/// A single reversible edit to the <see cref="Models.Composition"/>. Implementations must
/// make <see cref="Undo"/> restore exactly the state that existed before <see cref="Do"/>.
/// </summary>
public interface IUndoableAction
{
    /// <summary>Short human-readable label, e.g. "Elimina 2 fogli" (shown next to Undo/Redo).</summary>
    string Description { get; }

    void Do();

    void Undo();
}
