using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Combines the selected sheets into one: all their placements move onto a single new sheet
/// (in selection order), the originals are removed, and the merged sheet takes the position
/// of the first selection. This is the "N pages on one sheet" operation — the layout of the
/// merged placements is decided at compose/preview time by <see cref="Layout.SheetLayout"/>.
/// </summary>
public sealed class MergeSheetsAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly int[] _indices;
    private readonly List<(int Index, Sheet Sheet)> _removed = [];
    private Sheet? _merged;

    public MergeSheetsAction(Composition composition, IEnumerable<int> sheetIndices)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(sheetIndices);

        _composition = composition;
        _indices = [.. sheetIndices.Distinct().OrderBy(static i => i)];

        if (_indices.Length < 2)
        {
            throw new ArgumentException("Select at least two sheets to merge.", nameof(sheetIndices));
        }

        foreach (int index in _indices)
        {
            if (index < 0 || index >= composition.Sheets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetIndices), index, "Sheet index out of range.");
            }
        }
    }

    public string Description => $"Unisci {_indices.Length} pagine in un foglio";

    public void Do()
    {
        _merged ??= new Sheet(_indices.SelectMany(i => _composition.Sheets[i].Placements));

        int insertAt = _indices[0];

        _removed.Clear();
        for (int k = _indices.Length - 1; k >= 0; k--)
        {
            int index = _indices[k];
            _removed.Add((index, _composition.RemoveSheetAt(index)));
        }

        _composition.InsertSheet(insertAt, _merged);
    }

    public void Undo()
    {
        int mergedIndex = _composition.IndexOf(_merged!);
        if (mergedIndex >= 0)
        {
            _composition.RemoveSheetAt(mergedIndex);
        }

        // _removed holds descending indices; re-insert ascending to restore the original order.
        for (int k = _removed.Count - 1; k >= 0; k--)
        {
            (int index, Sheet sheet) = _removed[k];
            _composition.InsertSheet(index, sheet);
        }

        _removed.Clear();
    }
}
