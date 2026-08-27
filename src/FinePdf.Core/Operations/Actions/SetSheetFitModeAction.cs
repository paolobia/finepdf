using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Sets (or clears, with <c>null</c>) the per-sheet <see cref="Sheet.FitModeOverride"/> on one
/// or more sheets.
/// </summary>
public sealed class SetSheetFitModeAction : IUndoableAction
{
    private readonly IReadOnlyList<Sheet> _sheets;
    private readonly FitMode? _mode;
    private FitMode?[]? _previous;

    public SetSheetFitModeAction(IEnumerable<Sheet> sheets, FitMode? mode)
    {
        ArgumentNullException.ThrowIfNull(sheets);

        _sheets = [.. sheets];
        if (_sheets.Count == 0)
        {
            throw new ArgumentException("No sheets supplied.", nameof(sheets));
        }

        _mode = mode;
    }

    public string Description => _mode is null
        ? "Adattamento: usa predefinito"
        : $"Adattamento: {_mode}";

    public void Do()
    {
        _previous = new FitMode?[_sheets.Count];
        for (int i = 0; i < _sheets.Count; i++)
        {
            _previous[i] = _sheets[i].FitModeOverride;
            _sheets[i].FitModeOverride = _mode;
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
            _sheets[i].FitModeOverride = _previous[i];
        }
    }
}
