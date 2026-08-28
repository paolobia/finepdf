using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Inserts a blank sheet, backed by a synthetic single blank page sized to the document's
/// current paper (so it can take part in a merge / N-up as a genuine white cell).
/// </summary>
public sealed class InsertBlankSheetAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly int _index;
    private readonly Sheet _blank;

    public InsertBlankSheetAction(Composition composition, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(composition);

        _composition = composition;
        _index = index ?? composition.Sheets.Count;

        (double width, double height) = composition.SheetSizePoints;
        _blank = new Sheet(new Placement(SourceDocument.CreateBlank(width, height), 0));

        if (_index < 0 || _index > composition.Sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), _index, "Insert index out of range.");
        }
    }

    public string Description => "Inserisci pagina vuota";

    public void Do() => _composition.InsertSheet(Math.Min(_index, _composition.Sheets.Count), _blank);

    public void Undo()
    {
        int index = _composition.IndexOf(_blank);
        if (index >= 0)
        {
            _composition.RemoveSheetAt(index);
        }
    }
}
