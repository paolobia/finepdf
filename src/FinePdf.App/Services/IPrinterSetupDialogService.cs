using FinePdf.App.ViewModels;
using FinePdf.Core.Services;

namespace FinePdf.App.Services;

/// <summary>Shows the "Stampante virtuale" install/remove dialog.</summary>
public interface IPrinterSetupDialogService
{
    Task<PrinterSetupResult?> RequestAsync(PrinterInstallState state, string suggestedFolder);
}
