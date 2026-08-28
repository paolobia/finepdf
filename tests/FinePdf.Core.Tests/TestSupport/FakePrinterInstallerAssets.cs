using FinePdf.Core.Services;

namespace FinePdf.Core.Tests.TestSupport;

/// <summary>
/// Extracts trivial stub scripts into a <see cref="TempDir"/> so <see cref="LinuxPrinterManager"/>
/// can be tested without the real embedded packaging files.
/// </summary>
internal sealed class FakePrinterInstallerAssets : IPrinterInstallerAssets, IDisposable
{
    private readonly List<TempDir> _dirs = [];

    public string LastDir { get; private set; } = string.Empty;

    public string ExtractToStagingDirectory()
    {
        var dir = new TempDir();
        _dirs.Add(dir);
        foreach (string name in new[] { "finepdf", "finepdf.ppd", "install.sh", "uninstall.sh" })
        {
            File.WriteAllText(dir.File(name), "#!/bin/sh\nexit 0\n");
            if (!OperatingSystem.IsWindows() && name != "finepdf.ppd")
            {
                File.SetUnixFileMode(
                    dir.File(name),
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        LastDir = dir.Path;
        return dir.Path;
    }

    public void Dispose()
    {
        foreach (TempDir dir in _dirs)
        {
            dir.Dispose();
        }
    }
}
