using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>Changes the N-up cell-fill <see cref="Sheet.NUpOrder"/> on one or more sheets.</summary>
public sealed class SetNUpOrderAction : IUndoableAction
{
    private readonly IReadOnlyList<Sheet> _sheets;
    private readonly NUpOrder _order;
    private NUpOrder[]? _previous;

    public SetNUpOrderAction(IEnumerable<Sheet> sheets, NUpOrder order)
    {
        ArgumentNullException.ThrowIfNull(sheets);

        _sheets = [.. sheets];
        if (_sheets.Count == 0)
        {
            throw new ArgumentException("No sheets supplied.", nameof(sheets));
        }

        _order = order;
    }

    public string Description => _order == NUpOrder.ColumnMajor
        ? "Ordine N-up: per colonne"
        : "Ordine N-up: per righe";

    public void Do()
    {
        _previous = new NUpOrder[_sheets.Count];
        for (int i = 0; i < _sheets.Count; i++)
        {
            _previous[i] = _sheets[i].NUpOrder;
            _sheets[i].NUpOrder = _order;
        }
    }

    public void Undo()
    {
        if (_previous is null)
        {
            return;
        }

        for (int i = 0; i < _sheets.Count; i++)
        {
            _sheets[i].NUpOrder = _previous[i];
        }
    }
}
