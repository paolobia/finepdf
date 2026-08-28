using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;
using SkiaSharp;

namespace FinePdf.Core.Tests;

public class DocumentImportServiceTests
{
    private static string WritePng(string dir, string name, int w = 120, int h = 80)
    {
        using var bitmap = new SKBitmap(w, h);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.CornflowerBlue);
        }

        string path = Path.Combine(dir, name);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream file = File.Create(path);
        data.SaveTo(file);
        return path;
    }

    [Fact]
    public void EnsurePdf_ForImage_ProducesOnePagePdf()
    {
        using var dir = new TempDir();
        var service = new DocumentImportService();
        try
        {
            string png = WritePng(dir.Path, "photo.png");

            string pdf = service.EnsurePdf(png);

            Assert.EndsWith(".pdf", pdf, StringComparison.Ordinal);
            Assert.True(File.Exists(pdf));
            Assert.Equal(1, new PdfSharpDocumentService().Load(pdf).PageCount);
        }
        finally
        {
            service.CleanupSession();
        }
    }

    [Fact]
    public void EnsurePdf_ForPdf_ReturnsSamePath()
    {
        using var dir = new TempDir();
        string pdf = PdfTestFactory.CreatePdf(dir.Path, "in.pdf", 2);

        Assert.Equal(pdf, new DocumentImportService().EnsurePdf(pdf));
    }

    [Fact]
    public void ImportAsCopy_ForPdf_ReturnsCachedCopyThatSurvivesOriginalDeletion()
    {
        using var dir = new TempDir();
        var service = new DocumentImportService();
        try
        {
            string pdf = PdfTestFactory.CreatePdf(dir.Path, "in.pdf", 2);

            string copy = service.ImportAsCopy(pdf);

            Assert.NotEqual(pdf, copy);
            Assert.Equal(copy, service.ImportAsCopy(pdf));

            File.Delete(pdf);
            Assert.Equal(2, new PdfSharpDocumentService().Load(copy).PageCount);
        }
        finally
        {
            service.CleanupSession();
        }
    }

    [Fact]
    public void EnsurePdf_IsCached_SecondCallReturnsSameFile()
    {
        using var dir = new TempDir();
        var service = new DocumentImportService();
        try
        {
            string png = WritePng(dir.Path, "photo.png");
            Assert.Equal(service.EnsurePdf(png), service.EnsurePdf(png));
        }
        finally
        {
            service.CleanupSession();
        }
    }

    [Fact]
    public void CanImport_KnownAndUnknownExtensions()
    {
        var service = new DocumentImportService();
        Assert.True(service.CanImport("/x/a.pdf"));
        Assert.True(service.CanImport("/x/a.PNG"));
        Assert.True(service.CanImport("/x/a.webp"));
        Assert.False(service.CanImport("/x/a.txt"));
        Assert.False(service.CanImport("/x/a.docx"));
    }

    [Fact]
    public void EnsurePdf_CorruptImage_Throws()
    {
        using var dir = new TempDir();
        string bad = Path.Combine(dir.Path, "broken.png");
        File.WriteAllText(bad, "not really a png");

        Assert.Throws<DocumentImportException>(() => new DocumentImportService().EnsurePdf(bad));
    }
}
