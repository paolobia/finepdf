using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class FolderSourceServiceTests
{
    [Fact]
    public void SetFolder_ListsOnlyPdfFiles_Sorted()
    {
        using var dir = new TempDir();
        PdfTestFactory.CreatePdf(dir.Path, "b.pdf", 1);
        PdfTestFactory.CreatePdf(dir.Path, "a.pdf", 1);
        PdfTestFactory.CreatePdf(dir.Path, "c.PDF", 1);
        File.WriteAllText(dir.File("notes.txt"), "ignore me");

        using var service = new FolderSourceService(enableWatcher: false);
        service.SetFolder(dir.Path);

        Assert.Equal(
            ["a.pdf", "b.pdf", "c.PDF"],
            service.Files.Select(Path.GetFileName).ToArray());
    }

    [Fact]
    public void Refresh_AfterFileAdded_RaisesFilesChanged()
    {
        using var dir = new TempDir();
        PdfTestFactory.CreatePdf(dir.Path, "a.pdf", 1);

        using var service = new FolderSourceService(enableWatcher: false);
        service.SetFolder(dir.Path);

        int raised = 0;
        service.FilesChanged += (_, _) => raised++;

        PdfTestFactory.CreatePdf(dir.Path, "b.pdf", 1);
        service.Refresh();

        Assert.Equal(2, service.Files.Count);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Refresh_NoChange_DoesNotRaise()
    {
        using var dir = new TempDir();
        PdfTestFactory.CreatePdf(dir.Path, "a.pdf", 1);

        using var service = new FolderSourceService(enableWatcher: false);
        service.SetFolder(dir.Path);

        int raised = 0;
        service.FilesChanged += (_, _) => raised++;
        service.Refresh();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void SetFolder_MissingDirectory_Throws()
    {
        using var service = new FolderSourceService(enableWatcher: false);

        Assert.Throws<DirectoryNotFoundException>(
            () => service.SetFolder(Path.Combine(Path.GetTempPath(), "finepdf-missing-" + Guid.NewGuid().ToString("N"))));
    }
}
