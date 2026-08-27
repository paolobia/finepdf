using FinePdf.Core.Models;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class PdfComposerTests
{
    private readonly PdfComposer _composer = new();
    private readonly PdfSharpDocumentService _documents = new();

    [Fact]
    public void Compose_OneSheetPerPage_ProducesA4PortraitPages()
    {
        using var dir = new TempDir();
        SourceDocument source = _documents.Load(PdfTestFactory.CreatePdf(dir.Path, "src.pdf", 3));

        var composition = new Composition { PaperSize = PaperSize.A4, Orientation = PageOrientation.Portrait };
        new AddFilePagesAction(composition, source).Do();

        string outPath = dir.File("out.pdf");
        _composer.Compose(composition, outPath);

        SourceDocument result = _documents.Load(outPath);
        Assert.Equal(3, result.PageCount);
        Assert.Equal(PaperSize.A4.WidthPoints, result.Pages[0].WidthPoints, 1);
        Assert.Equal(PaperSize.A4.HeightPoints, result.Pages[0].HeightPoints, 1);
    }

    [Fact]
    public void Compose_LandscapeOrientation_SwapsSheetDimensions()
    {
        using var dir = new TempDir();
        SourceDocument source = _documents.Load(PdfTestFactory.CreatePdf(dir.Path, "src.pdf", 1));

        var composition = new Composition { PaperSize = PaperSize.A4, Orientation = PageOrientation.Landscape };
        new AddFilePagesAction(composition, source).Do();

        using var stream = new MemoryStream();
        _composer.Compose(composition, stream);
        stream.Position = 0;

        string outPath = dir.File("landscape.pdf");
        File.WriteAllBytes(outPath, stream.ToArray());
        SourceDocument result = _documents.Load(outPath);

        Assert.Equal(PaperSize.A4.HeightPoints, result.Pages[0].WidthPoints, 1);
        Assert.Equal(PaperSize.A4.WidthPoints, result.Pages[0].HeightPoints, 1);
    }

    [Fact]
    public void Compose_TwoPlacementsOnOneSheet_StaysOnePage()
    {
        using var dir = new TempDir();
        SourceDocument source = _documents.Load(PdfTestFactory.CreatePdf(dir.Path, "src.pdf", 2));

        var composition = new Composition();
        composition.AddSheet(new Sheet(
            new Placement(source, 0),
            new Placement(source, 1) { Rotation = new RotationDegrees(180) }));

        string outPath = dir.File("twoup.pdf");
        _composer.Compose(composition, outPath);

        Assert.Equal(1, _documents.Load(outPath).PageCount);
    }

    [Fact]
    public void Compose_BlankSheet_CountsAsAPage()
    {
        using var dir = new TempDir();
        SourceDocument source = _documents.Load(PdfTestFactory.CreatePdf(dir.Path, "src.pdf", 1));

        var composition = new Composition();
        composition.AddSheet(new Sheet(new Placement(source, 0)));
        composition.AddSheet(new Sheet());
        composition.AddSheet(new Sheet(new Placement(source, 0)));

        string outPath = dir.File("withblank.pdf");
        _composer.Compose(composition, outPath);

        Assert.Equal(3, _documents.Load(outPath).PageCount);
    }

    [Fact]
    public void Compose_NoSheets_Throws()
    {
        using var dir = new TempDir();
        var composition = new Composition();

        Assert.Throws<InvalidOperationException>(() => _composer.Compose(composition, dir.File("empty.pdf")));
    }
}
