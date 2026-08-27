namespace FinePdf.Core.Models;

/// <summary>
/// A named paper size expressed in PDF points (1/72 inch), in portrait orientation
/// (width &lt; height). Landscape is derived by swapping the two dimensions at the
/// <see cref="Composition"/> level.
/// </summary>
public sealed record PaperSize(string Name, double WidthPoints, double HeightPoints)
{
    public static readonly PaperSize A5 = new("A5", 419.53, 595.28);
    public static readonly PaperSize A4 = new("A4", 595.28, 841.89);
    public static readonly PaperSize A3 = new("A3", 841.89, 1190.55);
    public static readonly PaperSize Letter = new("Letter", 612d, 792d);
    public static readonly PaperSize Legal = new("Legal", 612d, 1008d);

    /// <summary>Presets offered in the UI. Custom sizes remain possible via the constructor.</summary>
    public static IReadOnlyList<PaperSize> Presets { get; } = [A4, A3, A5, Letter, Legal];

    public override string ToString() => Name;
}
