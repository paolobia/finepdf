using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>Moves one sheet from <paramref name="fromIndex"/> to <paramref name="toIndex"/>.</summary>
public sealed class MoveSheetAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly int _fromIndex;
    private readonly int _toIndex;

    public MoveSheetAction(Composition composition, int fromIndex, int toIndex)
    {
        ArgumentNullException.ThrowIfNull(composition);

        int count = composition.Sheets.Count;
        if (fromIndex < 0 || fromIndex >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromIndex), fromIndex, "Sheet index out of range.");
        }

        if (toIndex < 0 || toIndex >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex), toIndex, "Sheet index out of range.");
        }

        _composition = composition;
        _fromIndex = fromIndex;
        _toIndex = toIndex;
    }

    public string Description => "Sposta foglio";

    public void Do() => _composition.MoveSheet(_fromIndex, _toIndex);

    public void Undo() => _composition.MoveSheet(_toIndex, _fromIndex);
}
