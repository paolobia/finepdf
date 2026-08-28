namespace FinePdf.Core.Services;

/// <summary>
/// Picks the <see cref="IPrinterManager"/> for the current OS. Unlike
/// <see cref="PrintServiceFactory"/> it never throws off-Linux — it returns a null-object so the
/// UI can bind <see cref="IPrinterManager.IsSupported"/> to hide the feature.
/// </summary>
public static class PrinterManagerFactory
{
    public static IPrinterManager Create(IProcessRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        return OperatingSystem.IsLinux()
            ? new LinuxPrinterManager(runner, new EmbeddedPrinterInstallerAssets())
            : new UnsupportedPrinterManager();
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
