using System.ComponentModel;

namespace FinePdf.Core.Services;

/// <summary>
/// Linux implementation: checks the queue with <c>lpstat -v FinePdf</c> and runs the bundled
/// <c>install.sh</c>/<c>uninstall.sh</c> under <c>pkexec</c>. Every external call goes through
/// <see cref="IProcessRunner"/> for testability; mirrors <see cref="LinuxCupsPrintService"/>.
/// </summary>
public sealed class LinuxPrinterManager : IPrinterManager
{
    private const string PrinterName = "FinePdf";

    private readonly IProcessRunner _runner;
    private readonly IPrinterInstallerAssets _assets;

    public LinuxPrinterManager(IProcessRunner runner, IPrinterInstallerAssets assets)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(assets);
        _runner = runner;
        _assets = assets;
    }

    public bool IsSupported => true;

    public PrinterInstallState GetState()
    {
        try
        {
            ProcessResult result = _runner.Run("lpstat", ["-v", PrinterName]);
            return result.Succeeded && result.StandardOutput.Contains("finepdf:", StringComparison.Ordinal)
                ? PrinterInstallState.Installed
                : PrinterInstallState.NotInstalled;
        }
        catch (Win32Exception)
        {
            return PrinterInstallState.Unsupported;
        }
    }

    public void Install(string outputFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        if (!Path.IsPathRooted(outputFolder) || !Directory.Exists(outputFolder))
        {
            throw new PrinterManagerException(
                $"Cartella di output non valida: {outputFolder}", PrinterManagerFault.InstallFailed);
        }

        string stagingDirectory = _assets.ExtractToStagingDirectory();
        RunPrivileged(Path.Combine(stagingDirectory, "install.sh"), outputFolder);
    }

    public void Uninstall()
    {
        string stagingDirectory = _assets.ExtractToStagingDirectory();
        RunPrivileged(Path.Combine(stagingDirectory, "uninstall.sh"));
    }

    private void RunPrivileged(string scriptPath, string? argument = null)
    {
        var args = new List<string> { scriptPath };
        if (argument is not null)
        {
            args.Add(argument);
        }

        ProcessResult result;
        try
        {
            result = _runner.Run("pkexec", args);
        }
        catch (Win32Exception ex)
        {
            throw new PrinterManagerException(
                "Comando «pkexec» non trovato (installa polkit / policykit-1).",
                PrinterManagerFault.PkexecMissing, ex);
        }

        if (result.Succeeded)
        {
            return;
        }

        (PrinterManagerFault fault, string message) = result.ExitCode switch
        {
            3 => (PrinterManagerFault.SnapCupsUnsupported,
                "CUPS è installato come snap: la stampante virtuale non è supportata."),
            4 => (PrinterManagerFault.GhostscriptMissing,
                "Ghostscript non è installato."),
            5 => (PrinterManagerFault.CupsUnavailable,
                "CUPS non è disponibile."),
            6 => (PrinterManagerFault.InstallFailed,
                "Stampante registrata ma la stampa di prova non ha prodotto alcun PDF. " +
                "Vedi /var/log/cups/error_log."),
            126 or 127 => (PrinterManagerFault.PermissionDenied,
                "Autorizzazione negata o annullata."),
            _ => (PrinterManagerFault.InstallFailed, Describe(result)),
        };

        throw new PrinterManagerException(message, fault);
    }

    private static string Describe(ProcessResult result)
    {
        string detail = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput.Trim()
            : result.StandardError.Trim();
        return string.IsNullOrEmpty(detail)
            ? "Operazione sulla stampante non riuscita."
            : $"Operazione sulla stampante non riuscita: {detail}";
    }
}
