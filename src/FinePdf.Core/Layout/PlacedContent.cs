namespace FinePdf.Core.Layout;

/// <summary>
/// The result of laying a source page into a cell: where to draw it and how much to rotate it.
/// <para>
/// <see cref="Width"/> and <see cref="Height"/> are the drawn size <em>before</em> rotation.
/// The caller draws the page centred on (<see cref="CenterX"/>, <see cref="CenterY"/>),
/// rotated by <see cref="RotationDegrees"/> clockwise about that centre.
/// </para>
/// </summary>
public readonly record struct PlacedContent(
    double CenterX,
    double CenterY,
    double Width,
    double Height,
    int RotationDegrees);
