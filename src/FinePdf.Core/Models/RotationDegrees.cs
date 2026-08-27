namespace FinePdf.Core.Models;

/// <summary>
/// A rotation constrained to multiples of 90°, normalised to [0, 360).
/// Positive values rotate clockwise.
/// </summary>
public readonly record struct RotationDegrees
{
    public RotationDegrees(int degrees)
    {
        int normalised = ((degrees % 360) + 360) % 360;
        if (normalised % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "Rotation must be a multiple of 90°.");
        }

        Value = normalised;
    }

    /// <summary>Normalised rotation in degrees: one of 0, 90, 180, 270.</summary>
    public int Value { get; }

    public static RotationDegrees None => new(0);

    public bool IsQuarterTurn => Value is 90 or 270;

    public RotationDegrees RotatedBy(int deltaDegrees) => new(Value + deltaDegrees);

    public override string ToString() => $"{Value}°";
}
