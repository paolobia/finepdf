namespace FinePdf.Core.Models;

/// <summary>
/// The document being assembled: a print paper size + orientation (single for the whole
/// document in v1), a default fit mode, and an ordered list of <see cref="Sheet"/>.
/// Nothing is written to disk until <see cref="Services.IPdfComposer"/> runs.
/// </summary>
public sealed class Composition
{
    private readonly List<Sheet> _sheets = [];

    public PaperSize PaperSize { get; set; } = PaperSize.A4;

    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    public FitMode DefaultFitMode { get; set; } = FitMode.Fit;

    /// <summary>Page margins applied to every sheet. Default: none.</summary>
    public PageMargins Margins { get; set; } = PageMargins.Zero;

    public IReadOnlyList<Sheet> Sheets => _sheets;

    /// <summary>Sheet dimensions in points after applying <see cref="Orientation"/>.</summary>
    public (double Width, double Height) SheetSizePoints =>
        Orientation == PageOrientation.Portrait
            ? (PaperSize.WidthPoints, PaperSize.HeightPoints)
            : (PaperSize.HeightPoints, PaperSize.WidthPoints);

    public int IndexOf(Sheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return _sheets.IndexOf(sheet);
    }

    public void AddSheet(Sheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        _sheets.Add(sheet);
    }

    public void InsertSheet(int index, Sheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        _sheets.Insert(index, sheet);
    }

    public Sheet RemoveSheetAt(int index)
    {
        Sheet removed = _sheets[index];
        _sheets.RemoveAt(index);
        return removed;
    }

    /// <summary>Move the sheet at <paramref name="fromIndex"/> so that it ends up at <paramref name="toIndex"/>.</summary>
    public void MoveSheet(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        }

        if (toIndex < 0 || toIndex >= _sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        Sheet moved = _sheets[fromIndex];
        _sheets.RemoveAt(fromIndex);
        _sheets.Insert(toIndex, moved);
    }
}
