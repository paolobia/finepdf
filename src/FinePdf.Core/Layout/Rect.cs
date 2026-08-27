namespace FinePdf.Core.Layout;

/// <summary>
/// An axis-aligned rectangle in sheet coordinates: origin at the top-left of the sheet,
/// X to the right, Y downwards, units in PDF points.
/// </summary>
public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    public double CenterX => X + (Width / 2);

    public double CenterY => Y + (Height / 2);
}
