using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>Deletes one or more sheets (multi-selection), restoring them in place on undo.</summary>
public sealed class DeleteSheetsAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly int[] _indices;
    private readonly List<(int Index, Sheet Sheet)> _removed = [];

    public DeleteSheetsAction(Composition composition, IEnumerable<int> indices)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(indices);

        _composition = composition;
        _indices = [.. indices.Distinct().OrderBy(static i => i)];

        if (_indices.Length == 0)
        {
            throw new ArgumentException("No sheet indices supplied.", nameof(indices));
        }

        foreach (int index in _indices)
        {
            if (index < 0 || index >= composition.Sheets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(indices), index, "Sheet index out of range.");
            }
        }
    }

    public string Description => _indices.Length == 1 ? "Elimina foglio" : $"Elimina {_indices.Length} fogli";

    public void Do()
    {
        _removed.Clear();

        // Remove from the highest index down so the lower indices stay valid.
        for (int k = _indices.Length - 1; k >= 0; k--)
        {
            int index = _indices[k];
            Sheet sheet = _composition.RemoveSheetAt(index);
            _removed.Add((index, sheet));
        }
    }

    public void Undo()
    {
        // _removed holds descending indices; re-insert ascending to land each sheet back in place.
        for (int k = _removed.Count - 1; k >= 0; k--)
        {
            (int index, Sheet sheet) = _removed[k];
            _composition.InsertSheet(index, sheet);
        }

        _removed.Clear();
    }
}
