namespace FinePdf.Core.Services;

/// <summary>Whether the "FinePdf" virtual printer is currently registered with CUPS.</summary>
public enum PrinterInstallState
{
    /// <summary>The platform has no virtual-printer support (not Linux).</summary>
    Unsupported,

    NotInstalled,

    Installed,
}

/// <summary>
/// Installs / removes the "FinePdf" CUPS virtual printer. On Linux this drives a one-shot
/// <c>pkexec install.sh &lt;folder&gt;</c>; everywhere else it is a no-op reporting
/// <see cref="PrinterInstallState.Unsupported"/>.
/// </summary>
public interface IPrinterManager
{
    /// <summary><c>true</c> only on Linux. UI binds this to show/hide the "Stampante virtuale" button.</summary>
    bool IsSupported { get; }

    PrinterInstallState GetState();

    /// <exception cref="FinePdf.PrinterManagerException">The printer could not be installed.</exception>
    void Install(string outputFolder);

    /// <exception cref="FinePdf.PrinterManagerException">The printer could not be removed.</exception>
    void Uninstall();
}
