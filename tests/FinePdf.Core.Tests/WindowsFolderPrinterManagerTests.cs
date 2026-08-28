using FinePdf;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class WindowsFolderPrinterManagerTests
{
    [Fact]
    public void IsSupported_And_GetState()
    {
        var manager = new WindowsFolderPrinterManager();

        Assert.True(manager.IsSupported);
        Assert.Equal(PrinterInstallState.NotInstalled, manager.GetState());
    }

    [Fact]
    public void Install_WithExistingRootedFolder_DoesNotThrow()
    {
        using var dir = new TempDir();
        new WindowsFolderPrinterManager().Install(dir.Path);
    }

    [Fact]
    public void Install_WithRelativeFolder_ThrowsInstallFailed()
    {
        var ex = Assert.Throws<PrinterManagerException>(
            () => new WindowsFolderPrinterManager().Install("relative/path"));
        Assert.Equal(PrinterManagerFault.InstallFailed, ex.Fault);
    }

    [Fact]
    public void Install_WithBlankFolder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new WindowsFolderPrinterManager().Install(""));
    }

    [Fact]
    public void Install_WithMissingDirectory_ThrowsInstallFailed()
    {
        string missing = Path.Combine(Path.GetTempPath(), "finepdf-nope-" + Guid.NewGuid().ToString("N"));

        var ex = Assert.Throws<PrinterManagerException>(() => new WindowsFolderPrinterManager().Install(missing));
        Assert.Equal(PrinterManagerFault.InstallFailed, ex.Fault);
    }

    [Fact]
    public void Uninstall_DoesNotThrow()
    {
        new WindowsFolderPrinterManager().Uninstall();
    }
}
