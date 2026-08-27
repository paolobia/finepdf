using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Appends (or inserts at a given position) one single-placement <see cref="Sheet"/> for
/// every page of a <see cref="SourceDocument"/> — the "double-click a file adds all its pages"
/// gesture.
/// </summary>
public sealed class AddFilePagesAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly SourceDocument _source;
    private readonly int _insertIndex;
    private readonly List<Sheet> _createdSheets = [];

    public AddFilePagesAction(Composition composition, SourceDocument source, int? insertIndex = null)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(source);

        _composition = composition;
        _source = source;
        _insertIndex = insertIndex ?? composition.Sheets.Count;

        if (_insertIndex < 0 || _insertIndex > composition.Sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(insertIndex), _insertIndex, "Insert index out of range.");
        }
    }

    public string Description => _source.PageCount == 1
        ? $"Aggiungi «{_source.FileName}»"
        : $"Aggiungi «{_source.FileName}» ({_source.PageCount} pagine)";

    public void Do()
    {
        if (_createdSheets.Count == 0)
        {
            for (int i = 0; i < _source.PageCount; i++)
            {
                _createdSheets.Add(new Sheet(new Placement(_source, i)));
            }
        }

        for (int i = 0; i < _createdSheets.Count; i++)
        {
            _composition.InsertSheet(_insertIndex + i, _createdSheets[i]);
        }
    }

    public void Undo()
    {
        foreach (Sheet sheet in _createdSheets)
        {
            int index = _composition.IndexOf(sheet);
            if (index >= 0)
            {
                _composition.RemoveSheetAt(index);
            }
        }
    }
}
