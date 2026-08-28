namespace FinePdf.Core.Services;

/// <summary>
/// Picks the <see cref="IPrinterManager"/> for the current OS:
/// Linux → <see cref="LinuxPrinterManager"/> (installs a CUPS backend);
/// Windows → <see cref="WindowsFolderPrinterManager"/> (just points FinePdf at the folder a
/// third-party PDF printer saves to);
/// elsewhere → a null-object with <see cref="IPrinterManager.IsSupported"/> false so the UI
/// hides the feature. Never throws.
/// </summary>
public static class PrinterManagerFactory
{
    public static IPrinterManager Create(IProcessRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        if (OperatingSystem.IsWindows())
        {
            return new WindowsFolderPrinterManager();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxPrinterManager(runner, new EmbeddedPrinterInstallerAssets());
        }

        return new UnsupportedPrinterManager();
    }

    private sealed class UnsupportedPrinterManager : IPrinterManager
    {
        public bool IsSupported => false;

        public PrinterInstallState GetState() => PrinterInstallState.Unsupported;

        public void Install(string outputFolder) => throw Unsupported();

        public void Uninstall() => throw Unsupported();

        private static PrinterManagerException Unsupported() =>
            new("La stampante virtuale è disponibile solo su Linux.", PrinterManagerFault.Unsupported);
    }
}
