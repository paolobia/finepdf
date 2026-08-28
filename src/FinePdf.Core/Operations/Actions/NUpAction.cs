using FinePdf.Core.Layout;
using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Imposes the selected sheets as "N pages per sheet": every placement of the selection is
/// collected in order and re-chunked onto new sheets of <see cref="_perSheet"/> placements
/// each (2 or 4). The originals are removed and the new sheets take the position of the
/// first selection. Unlike <see cref="MergeSheetsAction"/> (which always produces a single
/// sheet), this keeps producing sheets until every page is placed.
/// </summary>
public sealed class NUpAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly int[] _indices;
    private readonly int _perSheet;
    private readonly NUpOrder _order;
    private readonly bool _autoOrient;
    private readonly List<(int Index, Sheet Sheet)> _removed = [];
    private List<Sheet>? _created;

    /// <param name="autoOrient">
    /// When <c>true</c> (default) each placement is rotated 90° if that makes it fill its cell
    /// better — a portrait page in a landscape 2-up cell, etc.
    /// </param>
    public NUpAction(
        Composition composition,
        IEnumerable<int> sheetIndices,
        int perSheet,
        NUpOrder order = NUpOrder.RowMajor,
        bool autoOrient = true)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(sheetIndices);

        if (perSheet is not (2 or 4))
        {
            throw new ArgumentOutOfRangeException(
                nameof(perSheet), perSheet, "N-up supports 2 or 4 pages per sheet.");
        }

        _composition = composition;
        _perSheet = perSheet;
        _order = order;
        _autoOrient = autoOrient;
        _indices = [.. sheetIndices.Distinct().OrderBy(static i => i)];

        if (_indices.Length < 2)
        {
            throw new ArgumentException("Select at least two sheets for N-up.", nameof(sheetIndices));
        }

        foreach (int index in _indices)
        {
            if (index < 0 || index >= composition.Sheets.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetIndices), index, "Sheet index out of range.");
            }
        }
    }

    public string Description => $"{_perSheet} pagine per foglio";

    public void Do()
    {
        if (_created is null)
        {
            List<Placement> placements = [.. _indices.SelectMany(i => _composition.Sheets[i].Placements)];
            (double sheetWidth, double sheetHeight) = _composition.SheetSizePoints;
            _created = [];
            for (int start = 0; start < placements.Count; start += _perSheet)
            {
                List<Placement> chunk = [.. placements.Skip(start).Take(_perSheet)];
                if (_autoOrient)
                {
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        Layout.Rect cell = SheetLayout.CellFor(
                            chunk.Count, i, sheetWidth, sheetHeight, _order, _composition.Margins);
                        int fix = SheetLayout.OrientationFixupDegrees(
                            cell, chunk[i].Page.WidthPoints, chunk[i].Page.HeightPoints, chunk[i].Rotation.Value);
                        if (fix != 0)
                        {
                            chunk[i] = chunk[i] with { Rotation = chunk[i].Rotation.RotatedBy(fix) };
                        }
                    }
                }

                _created.Add(new Sheet(chunk) { NUpOrder = _order });
            }
        }

        int insertAt = _indices[0];

        _removed.Clear();
        for (int k = _indices.Length - 1; k >= 0; k--)
        {
            int index = _indices[k];
            _removed.Add((index, _composition.RemoveSheetAt(index)));
        }

        for (int i = 0; i < _created.Count; i++)
        {
            _composition.InsertSheet(insertAt + i, _created[i]);
        }
    }

    public void Undo()
    {
        foreach (Sheet sheet in _created!)
        {
            int index = _composition.IndexOf(sheet);
            if (index >= 0)
            {
                _composition.RemoveSheetAt(index);
            }
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
