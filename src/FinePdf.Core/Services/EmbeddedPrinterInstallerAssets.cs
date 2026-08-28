using System.Reflection;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class EmbeddedPrinterInstallerAssets : IPrinterInstallerAssets
{
    private const string ResourcePrefix = "FinePdf.Core.Packaging.Linux.";

    private static readonly HashSet<string> Executable = new(StringComparer.Ordinal)
    {
        "finepdf", "install.sh", "uninstall.sh",
    };

    private readonly string? _stagingOverride;

    /// <param name="stagingOverride">Test hook: where to extract. <c>null</c> = the real per-user location.</param>
    public EmbeddedPrinterInstallerAssets(string? stagingOverride = null)
    {
        _stagingOverride = stagingOverride;
    }

    public string ExtractToStagingDirectory()
    {
        Assembly assembly = typeof(EmbeddedPrinterInstallerAssets).Assembly;
        string[] names = [.. assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))];

        if (names.Length == 0)
        {
            throw new PrinterManagerException(
                "File di installazione della stampante non inclusi nella build.",
                PrinterManagerFault.InstallFailed);
        }

        string stagingDirectory = _stagingOverride ?? Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify),
            "FinePdf",
            "installer");

        if (Directory.Exists(stagingDirectory))
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }

        Directory.CreateDirectory(stagingDirectory);

        foreach (string resourceName in names)
        {
            string leaf = resourceName[ResourcePrefix.Length..];
            string destination = Path.Combine(stagingDirectory, leaf);

            using (Stream source = assembly.GetManifestResourceStream(resourceName)
                ?? throw new PrinterManagerException(
                    $"Risorsa mancante: {resourceName}", PrinterManagerFault.InstallFailed))
            using (FileStream target = File.Create(destination))
            {
                source.CopyTo(target);
            }

            if (!OperatingSystem.IsWindows() && Executable.Contains(leaf))
            {
                File.SetUnixFileMode(
                    destination,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }

        return stagingDirectory;
    }
}
