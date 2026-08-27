using FinePdf.Core.Models;

namespace FinePdf.Core.Services;

/// <summary>A printer available to the current user.</summary>
public sealed record PrinterInfo(string Name, bool IsDefault)
{
    public override string ToString() => IsDefault ? $"{Name} (predefinita)" : Name;
}

/// <summary>
/// Sends a <see cref="Composition"/> to a physical printer. Implementations are chosen per
/// platform by <see cref="PrintServiceFactory"/>; the composed output matches the on-screen
/// preview because both go through <see cref="IPdfComposer"/> / <see cref="Layout.SheetLayout"/>.
/// </summary>
public interface IPrintService
{
    /// <exception cref="FinePdf.PrintException">The printer list could not be retrieved.</exception>
    IReadOnlyList<PrinterInfo> GetPrinters();

    /// <exception cref="FinePdf.PrintException">The job could not be submitted.</exception>
    void Print(Composition composition, PrintOptions options);
}
