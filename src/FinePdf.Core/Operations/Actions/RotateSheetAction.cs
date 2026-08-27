using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>
/// Rotates the content of a sheet by a multiple of 90°. By default every placement on the
/// sheet is rotated; pass <paramref name="placementIndex"/> to rotate just one cell of an
/// N-up sheet.
/// </summary>
public sealed class RotateSheetAction : IUndoableAction
{
    private readonly Sheet _sheet;
    private readonly int _deltaDegrees;
    private readonly int? _placementIndex;
    private (int Index, RotationDegrees Rotation)[]? _previous;

    public RotateSheetAction(Sheet sheet, int deltaDegrees, int? placementIndex = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (deltaDegrees % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaDegrees), deltaDegrees, "Rotation must be a multiple of 90°.");
        }

        if (sheet.IsBlank)
        {
            throw new InvalidOperationException("A blank sheet has no content to rotate.");
        }

        if (placementIndex is { } index && (index < 0 || index >= sheet.Placements.Count))
        {
            throw new ArgumentOutOfRangeException(nameof(placementIndex), index, "Placement index out of range.");
        }

        _sheet = sheet;
        _deltaDegrees = deltaDegrees;
        _placementIndex = placementIndex;
    }

    public string Description => (((_deltaDegrees % 360) + 360) % 360) switch
    {
        90 => "Ruota 90° in senso orario",
        270 => "Ruota 90° in senso antiorario",
        _ => "Ruota di 180°",
    };

    public void Do()
    {
        IEnumerable<int> targets = _placementIndex is { } only
            ? [only]
            : Enumerable.Range(0, _sheet.Placements.Count);

        var snapshot = new List<(int, RotationDegrees)>();
        foreach (int i in targets)
        {
            Placement placement = _sheet.Placements[i];
            snapshot.Add((i, placement.Rotation));
            _sheet.ReplacePlacement(i, placement with { Rotation = placement.Rotation.RotatedBy(_deltaDegrees) });
        }

        _previous = [.. snapshot];
    }

    public void Undo()
    {
        if (_previous is null)
        {
            return;
        }

        foreach ((int index, RotationDegrees rotation) in _previous)
        {
            _sheet.ReplacePlacement(index, _sheet.Placements[index] with { Rotation = rotation });
        }
    }
}
