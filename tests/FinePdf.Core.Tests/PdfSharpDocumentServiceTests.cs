using FinePdf.Core;
using FinePdf.Core.Models;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class PdfSharpDocumentServiceTests
{
    private readonly PdfSharpDocumentService _service = new();

    [Fact]
    public void Load_ReturnsPageCountAndSizes()
    {
        using var dir = new TempDir();
        string path = PdfTestFactory.CreatePdfWithPageSizes(
            dir.Path,
            "sample.pdf",
            (200, 400),
            (400, 200),
            (300, 300));

        SourceDocument doc = _service.Load(path);

        Assert.Equal("sample.pdf", doc.FileName);
        Assert.Equal(3, doc.PageCount);
        Assert.Equal(200, doc.Pages[0].WidthPoints, 1);
        Assert.Equal(400, doc.Pages[0].HeightPoints, 1);
        Assert.Equal(400, doc.Pages[1].WidthPoints, 1);
        Assert.Equal(200, doc.Pages[1].HeightPoints, 1);
    }

    [Fact]
    public void Load_MissingFile_ThrowsPdfLoadException()
    {
        using var dir = new TempDir();

        PdfLoadException ex = Assert.Throws<PdfLoadException>(() => _service.Load(dir.File("nope.pdf")));

        Assert.False(ex.IsPasswordProtected);
    }

    [Fact]
    public void Load_NotAPdf_ThrowsPdfLoadException()
    {
        using var dir = new TempDir();
        string path = dir.File("garbage.pdf");
        File.WriteAllText(path, "this is definitely not a pdf");

        Assert.Throws<PdfLoadException>(() => _service.Load(path));
    }

    [Fact]
    public void Load_BlankArgument_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.Load("   "));
    }
}
