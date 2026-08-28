namespace FinePdf.Core.Models;

/// <summary>
/// Page margins in PDF points (1/72"). The composer and preview keep the sheet at its full
/// paper size but lay content only inside the sheet minus these margins.
/// </summary>
public sealed record PageMargins
{
    private const double PointsPerMillimeter = 72.0 / 25.4;

    public PageMargins(double left, double top, double right, double bottom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(top);
        ArgumentOutOfRangeException.ThrowIfNegative(right);
        ArgumentOutOfRangeException.ThrowIfNegative(bottom);

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left { get; }

    public double Top { get; }

    public double Right { get; }

    public double Bottom { get; }

    public static PageMargins Zero { get; } = new(0, 0, 0, 0);

    public bool IsZero => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    /// <summary>Same margin on all four edges, given in points.</summary>
    public static PageMargins Uniform(double points) => new(points, points, points, points);

    /// <summary>Same margin on all four edges, given in millimetres.</summary>
    public static PageMargins FromMillimeters(double millimeters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(millimeters);
        return Uniform(millimeters * PointsPerMillimeter);
    }

    /// <summary>The largest single edge margin, expressed in millimetres (for round-tripping a uniform value into the UI).</summary>
    public double MaxEdgeMillimeters => Math.Max(Math.Max(Left, Right), Math.Max(Top, Bottom)) / PointsPerMillimeter;
}
