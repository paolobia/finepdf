using FinePdf.Core.Layout;
using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Rotates each placement of the given sheets by 90° where that makes it match its cell's
/// orientation (portrait page → portrait cell). A no-op for placements already oriented right.
/// Useful after "unisci in un unico foglio" or on a single-page sheet whose page is turned
/// the wrong way for the paper.
/// </summary>
public sealed class FitOrientationAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly IReadOnlyList<Sheet> _sheets;
    private readonly List<(Sheet Sheet, int Index, RotationDegrees Rotation)> _previous = [];

    public FitOrientationAction(Composition composition, IEnumerable<Sheet> sheets)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(sheets);

        _composition = composition;
        _sheets = [.. sheets];
        if (_sheets.Count == 0)
        {
            throw new ArgumentException("No sheets supplied.", nameof(sheets));
        }
    }

    public string Description => "Adatta orientamento al foglio";

    public void Do()
    {
        _previous.Clear();
        (double sheetWidth, double sheetHeight) = _composition.SheetSizePoints;

        foreach (Sheet sheet in _sheets)
        {
            int count = sheet.Placements.Count;
            for (int i = 0; i < count; i++)
            {
                Placement placement = sheet.Placements[i];
                Rect cell = SheetLayout.CellFor(
                    count, i, sheetWidth, sheetHeight, sheet.NUpOrder, _composition.Margins);
                int fix = SheetLayout.OrientationFixupDegrees(
                    cell, placement.Page.WidthPoints, placement.Page.HeightPoints, placement.Rotation.Value);
                if (fix == 0)
                {
                    continue;
                }

                _previous.Add((sheet, i, placement.Rotation));
                sheet.ReplacePlacement(i, placement with { Rotation = placement.Rotation.RotatedBy(fix) });
            }
        }
    }

    public void Undo()
    {
        for (int k = _previous.Count - 1; k >= 0; k--)
        {
            (Sheet sheet, int index, RotationDegrees rotation) = _previous[k];
            sheet.ReplacePlacement(index, sheet.Placements[index] with { Rotation = rotation });
        }

        _previous.Clear();
    }
}
