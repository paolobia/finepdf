namespace FinePdf.Core.Services;

/// <summary>
/// Materialises the CUPS packaging files (backend, PPD, install/uninstall scripts) onto disk so
/// <see cref="LinuxPrinterManager"/> can hand a script path to <c>pkexec</c>.
/// </summary>
public interface IPrinterInstallerAssets
{
    /// <summary>
    /// Extracts the packaging files into a fresh user-owned staging directory (wiped and
    /// recreated on each call), makes the scripts and backend executable, and returns the
    /// directory path. It contains at least <c>install.sh</c> and <c>uninstall.sh</c>.
    /// </summary>
    string ExtractToStagingDirectory();
}
