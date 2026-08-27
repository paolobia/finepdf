namespace FinePdf.Core.Models;

/// <summary>
/// Metadata for a single page of a <see cref="SourceDocument"/>. Sizes are in PDF points
/// and already account for the page's intrinsic <c>/Rotate</c> entry, so they describe the
/// page as a reader would see it.
/// </summary>
public sealed record SourcePage(int Index, double WidthPoints, double HeightPoints);
