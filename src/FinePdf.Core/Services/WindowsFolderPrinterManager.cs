namespace FinePdf.Core.Services;

/// <summary>
/// Windows has no FinePdf-installed virtual printer: the user installs a free third-party PDF
/// printer (Xodo PDF Printer, Documalis Free PDF Printer, …) that saves to a folder without a
/// prompt, and FinePdf just watches that folder via the "PDF da stampante" tab. This manager
/// therefore only validates the chosen folder; the ViewModel persists it and points the tab at it.
/// </summary>
public sealed class WindowsFolderPrinterManager : IPrinterManager
{
    public bool IsSupported => true;

    // Nothing is "installed" by us, so the state is always NotInstalled: the setup dialog then
    // always shows the folder picker + confirm button and never a "Rimuovi" button.
    public PrinterInstallState GetState() => PrinterInstallState.NotInstalled;

    public void Install(string outputFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        if (!Path.IsPathRooted(outputFolder) || !Directory.Exists(outputFolder))
        {
            throw new PrinterManagerException(
                $"Cartella non valida: {outputFolder}", PrinterManagerFault.InstallFailed);
        }
    }

    public void Uninstall()
    {
        // Nothing to undo — the output folder is user data.
    }
}
