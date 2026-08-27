namespace FinePdf.Core.Models;

/// <summary>
/// One piece of source content placed on a <see cref="Sheet"/>: a single page of a
/// <see cref="SourceDocument"/>, optionally rotated. A sheet with one placement is a normal
/// page; two or four placements is an "N pages on one sheet" imposition.
/// </summary>
public sealed record Placement
{
    public Placement(SourceDocument source, int sourcePageIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (sourcePageIndex < 0 || sourcePageIndex >= source.PageCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourcePageIndex), sourcePageIndex,
                $"'{source.FileName}' has {source.PageCount} page(s).");
        }

        Source = source;
        SourcePageIndex = sourcePageIndex;
    }

    public SourceDocument Source { get; }

    /// <summary>Zero-based index into <see cref="Models.SourceDocument.Pages"/>.</summary>
    public int SourcePageIndex { get; }

    /// <summary>Rotation applied on top of the page's intrinsic orientation.</summary>
    public RotationDegrees Rotation { get; init; } = RotationDegrees.None;

    public SourcePage Page => Source.Pages[SourcePageIndex];
}
