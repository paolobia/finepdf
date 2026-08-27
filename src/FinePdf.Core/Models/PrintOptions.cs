namespace FinePdf.Core.Models;

/// <summary>
/// A print request: which printer, how many copies, one/two-sided, and which sheets.
/// </summary>
public sealed record PrintOptions
{
    public PrintOptions(string printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
        PrinterName = printerName;
    }

    public string PrinterName { get; }

    /// <summary>Number of copies (at least 1).</summary>
    public int Copies { get; init; } = 1;

    public DuplexMode Duplex { get; init; } = DuplexMode.Simplex;

    /// <summary>Which sheets to print. Defaults to every sheet.</summary>
    public PageRange Range { get; init; } = PageRange.All;

    /// <summary>Validates the option values, throwing <see cref="ArgumentException"/> on nonsense.</summary>
    public PrintOptions Validated()
    {
        if (Copies < 1)
        {
            throw new ArgumentException("Il numero di copie deve essere almeno 1.", nameof(Copies));
        }

        return this;
    }
}
