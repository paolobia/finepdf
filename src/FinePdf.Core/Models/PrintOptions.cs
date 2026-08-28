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

    /// <summary>
    /// Extra driver options as <c>key=value</c> strings — passed straight through to the
    /// backend (CUPS: one <c>-o key=value</c> each). Lets the user reach any printer setting
    /// the built-in fields don't cover (media, resolution, color model, fit-to-page…).
    /// </summary>
    public IReadOnlyList<string> RawOptions { get; init; } = [];

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
