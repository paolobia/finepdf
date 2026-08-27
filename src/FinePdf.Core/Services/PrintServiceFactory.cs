using FinePdf;

namespace FinePdf.Core.Services;

/// <summary>
/// Picks the <see cref="IPrintService"/> implementation for the current OS:
/// Windows → GDI via <c>WindowsPrintService</c> (only in the <c>net8.0-windows</c> build);
/// Linux → <see cref="LinuxCupsPrintService"/>.
/// </summary>
public static class PrintServiceFactory
{
    public static IPrintService Create(IProcessRunner runner, IPdfComposer composer)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(composer);

        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            return new WindowsPrintService(composer);
#else
            throw new PrintException(
                "La stampa su Windows richiede la build 'net8.0-windows' dell'applicazione.");
#endif
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxCupsPrintService(runner, composer);
        }

        throw new PrintException("Stampa non supportata su questa piattaforma.");
    }
}
