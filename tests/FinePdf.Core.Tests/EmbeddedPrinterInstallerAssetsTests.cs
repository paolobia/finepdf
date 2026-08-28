using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class EmbeddedPrinterInstallerAssetsTests
{
    [Fact]
    public void ExtractToStagingDirectory_WritesAllPackagingFiles()
    {
        using var dir = new TempDir();
        string staging = Path.Combine(dir.Path, "installer");

        string result = new EmbeddedPrinterInstallerAssets(staging).ExtractToStagingDirectory();

        Assert.Equal(staging, result);
        foreach (string name in new[] { "finepdf", "finepdf.ppd", "install.sh", "uninstall.sh" })
        {
            Assert.True(File.Exists(Path.Combine(staging, name)), $"missing {name}");
        }

        string backend = File.ReadAllText(Path.Combine(staging, "finepdf"));
        Assert.Contains("CUPS backend", backend, StringComparison.Ordinal);

        if (!OperatingSystem.IsWindows())
        {
            UnixFileMode mode = File.GetUnixFileMode(Path.Combine(staging, "install.sh"));
            Assert.True(mode.HasFlag(UnixFileMode.UserExecute));
        }
    }

    [Fact]
    public void ExtractToStagingDirectory_IsIdempotent()
    {
        using var dir = new TempDir();
        string staging = Path.Combine(dir.Path, "installer");
        var assets = new EmbeddedPrinterInstallerAssets(staging);

        assets.ExtractToStagingDirectory();
        File.WriteAllText(Path.Combine(staging, "stale.txt"), "x");
        assets.ExtractToStagingDirectory();

        Assert.False(File.Exists(Path.Combine(staging, "stale.txt")));
        Assert.True(File.Exists(Path.Combine(staging, "install.sh")));
    }
}
