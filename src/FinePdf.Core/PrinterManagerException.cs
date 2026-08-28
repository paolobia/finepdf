namespace FinePdf.Core;

/// <summary>Why a <see cref="Services.IPrinterManager"/> operation failed.</summary>
public enum PrinterManagerFault
{
    /// <summary>Not running on Linux.</summary>
    Unsupported,

    /// <summary><c>pkexec</c> is not installed.</summary>
    PkexecMissing,

    /// <summary>The polkit prompt was dismissed or the user is not authorised.</summary>
    PermissionDenied,

    /// <summary>CUPS client tools (<c>lpadmin</c>/<c>lpstat</c>) are missing.</summary>
    CupsUnavailable,

    /// <summary>CUPS is packaged as a snap, which cannot see a host backend.</summary>
    SnapCupsUnsupported,

    /// <summary>Ghostscript is not installed.</summary>
    GhostscriptMissing,

    /// <summary>The installer script failed for some other reason.</summary>
    InstallFailed,
}

/// <summary>
/// Raised by <see cref="Services.IPrinterManager"/> when installing or removing the "FinePdf"
/// virtual printer fails. The UI maps <see cref="Fault"/> to a status message.
/// </summary>
public sealed class PrinterManagerException : Exception
{
    public PrinterManagerException()
    {
    }

    public PrinterManagerException(string message)
        : base(message)
    {
    }

    public PrinterManagerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PrinterManagerException(string message, PrinterManagerFault fault, Exception? innerException = null)
        : base(message, innerException)
    {
        Fault = fault;
    }

    public PrinterManagerFault Fault { get; } = PrinterManagerFault.InstallFailed;
}
