using FinePdf;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class LinuxPrinterManagerTests
{
    private static (LinuxPrinterManager Manager, FakeProcessRunner Runner, FakePrinterInstallerAssets Assets) Build()
    {
        var runner = new FakeProcessRunner();
        var assets = new FakePrinterInstallerAssets();
        return (new LinuxPrinterManager(runner, assets), runner, assets);
    }

    [Fact]
    public void GetState_WhenLpstatShowsQueue_ReturnsInstalled()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();
        runner.Returns("lpstat", 0, stdout: "device for FinePdf: finepdf:/\n");

        Assert.Equal(PrinterInstallState.Installed, manager.GetState());
    }

    [Fact]
    public void GetState_WhenLpstatNonZero_ReturnsNotInstalled()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();
        runner.Returns("lpstat", 1, stderr: "lpstat: Unknown destination");

        Assert.Equal(PrinterInstallState.NotInstalled, manager.GetState());
    }

    [Fact]
    public void GetState_WhenLpstatMissing_ReturnsUnsupported()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();
        runner.NotInstalled("lpstat");

        Assert.Equal(PrinterInstallState.Unsupported, manager.GetState());
    }

    [Fact]
    public void Install_BuildsExactPkexecArgv()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, FakePrinterInstallerAssets assets) = Build();
        using var folder = new TempDir();
        runner.Returns("pkexec", 0);

        manager.Install(folder.Path);

        string[] args = runner.ArgsFor("pkexec");
        Assert.Equal(Path.Combine(assets.LastDir, "install.sh"), args[0]);
        Assert.Equal(folder.Path, args[1]);
    }

    [Fact]
    public void Uninstall_BuildsExactPkexecArgv()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, FakePrinterInstallerAssets assets) = Build();
        runner.Returns("pkexec", 0);

        manager.Uninstall();

        string[] args = runner.ArgsFor("pkexec");
        Assert.Single(args);
        Assert.Equal(Path.Combine(assets.LastDir, "uninstall.sh"), args[0]);
    }

    [Theory]
    [InlineData(126, PrinterManagerFault.PermissionDenied)]
    [InlineData(127, PrinterManagerFault.PermissionDenied)]
    [InlineData(3, PrinterManagerFault.SnapCupsUnsupported)]
    [InlineData(4, PrinterManagerFault.GhostscriptMissing)]
    [InlineData(5, PrinterManagerFault.CupsUnavailable)]
    [InlineData(6, PrinterManagerFault.InstallFailed)]
    public void Install_MapsScriptExitCodeToFault(int exit, PrinterManagerFault expected)
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();
        using var folder = new TempDir();
        runner.Returns("pkexec", exit, stderr: "boom");

        PrinterManagerException ex = Assert.Throws<PrinterManagerException>(() => manager.Install(folder.Path));
        Assert.Equal(expected, ex.Fault);
    }

    [Fact]
    public void Install_OtherExit_ThrowsInstallFailedWithStderr()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();
        using var folder = new TempDir();
        runner.Returns("pkexec", 9, stderr: "profilo AppArmor rotto");

        PrinterManagerException ex = Assert.Throws<PrinterManagerException>(() => manager.Install(folder.Path));
        Assert.Equal(PrinterManagerFault.InstallFailed, ex.Fault);
        Assert.Contains("AppArmor", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Install_WhenPkexecMissing_ThrowsPkexecMissing()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();
        using var folder = new TempDir();
        runner.NotInstalled("pkexec");

        PrinterManagerException ex = Assert.Throws<PrinterManagerException>(() => manager.Install(folder.Path));
        Assert.Equal(PrinterManagerFault.PkexecMissing, ex.Fault);
    }

    [Fact]
    public void Install_WhenFolderMissing_ThrowsBeforeInvokingPkexec()
    {
        (LinuxPrinterManager manager, FakeProcessRunner runner, _) = Build();

        Assert.Throws<PrinterManagerException>(
            () => manager.Install(Path.Combine(Path.GetTempPath(), "finepdf-nope-" + Guid.NewGuid().ToString("N"))));
        Assert.DoesNotContain(runner.Calls, c => c.FileName == "pkexec");
    }

    [Fact]
    public void Factory_ReportsSupportMatchingPlatform()
    {
        Assert.Equal(OperatingSystem.IsLinux(), PrinterManagerFactory.Create(new FakeProcessRunner()).IsSupported);
    }
}
