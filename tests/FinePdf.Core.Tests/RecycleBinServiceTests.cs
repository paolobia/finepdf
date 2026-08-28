using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class RecycleBinServiceTests
{
    [Fact]
    public void TryMoveToTrash_MissingFile_ReturnsFalse()
    {
        var service = new RecycleBinService(new FakeProcessRunner());

        Assert.False(service.TryMoveToTrash(Path.Combine(Path.GetTempPath(), "nope-" + Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public void TryMoveToTrash_ViaGio_BuildsArgvAndSucceeds()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var dir = new TempDir();
        string file = dir.File("doc.pdf");
        File.WriteAllText(file, "x");

        var runner = new FakeProcessRunner().Returns("gio", 0);
        var service = new RecycleBinService(runner);

        Assert.True(service.TryMoveToTrash(file));
        Assert.Equal(["trash", "--", file], runner.ArgsFor("gio"));
    }

    [Fact]
    public void TryMoveToTrash_GioMissing_FallsBackToFreedesktopSpec()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var src = new TempDir();
        using var trash = new TempDir();
        string file = src.File("relazione.pdf");
        File.WriteAllText(file, "hello");

        var service = new RecycleBinService(
            new FakeProcessRunner().NotInstalled("gio"),
            linuxTrashRootOverride: trash.Path);

        Assert.True(service.TryMoveToTrash(file));
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(trash.Path, "files", "relazione.pdf")));

        string info = File.ReadAllText(Path.Combine(trash.Path, "info", "relazione.pdf.trashinfo"));
        Assert.Contains("[Trash Info]", info, StringComparison.Ordinal);
        Assert.Contains("Path=", info, StringComparison.Ordinal);
        Assert.Contains("DeletionDate=", info, StringComparison.Ordinal);
    }

    [Fact]
    public void TryMoveToTrash_FreedesktopSpec_NameCollision_GetsSuffix()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var src = new TempDir();
        using var trash = new TempDir();
        Directory.CreateDirectory(Path.Combine(trash.Path, "files"));
        File.WriteAllText(Path.Combine(trash.Path, "files", "a.pdf"), "old");

        string file = src.File("a.pdf");
        File.WriteAllText(file, "new");

        var service = new RecycleBinService(
            new FakeProcessRunner().NotInstalled("gio"), linuxTrashRootOverride: trash.Path);

        Assert.True(service.TryMoveToTrash(file));
        Assert.True(File.Exists(Path.Combine(trash.Path, "files", "a_2.pdf")));
    }
}
