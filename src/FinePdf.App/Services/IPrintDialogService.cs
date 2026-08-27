using FinePdf.Core.Models;
using FinePdf.Core.Services;

namespace FinePdf.App.Services;

/// <summary>Shows the internal print dialog and returns the chosen options (or <c>null</c>).</summary>
public interface IPrintDialogService
{
    Task<PrintOptions?> RequestAsync(IReadOnlyList<PrinterInfo> printers);
}
