using FinePdf.Core.Models;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class SkiaSheetRenderServiceTests
{
    private readonly PdfSharpDocumentService _documents = new();

    private (Composition Composition, Sheet Sheet) OneSheet(string dir)
    {
        SourceDocument source = _documents.Load(PdfTestFactory.CreatePdf(dir, "src.pdf", 1));
        var composition = new Composition { PaperSize = PaperSize.A4, Orientation = PageOrientation.Portrait };
        new AddFilePagesAction(composition, source).Do();
        return (composition, composition.Sheets[0]);
    }

    [Fact]
    public void BrightenContent_DefaultsToTrue()
    {
        Assert.True(new SkiaSheetRenderService().BrightenContent);
    }

    [Fact]
    public void RenderSheetPng_BrightenToggle_ChangesOutputAndIsCachedPerMode()
    {
        using var dir = new TempDir();
        (Composition composition, Sheet sheet) = OneSheet(dir.Path);
        var service = new SkiaSheetRenderService();

        byte[] bright = service.RenderSheetPng(composition, sheet, 200);
        byte[] brightAgain = service.RenderSheetPng(composition, sheet, 200);
        Assert.Same(bright, brightAgain); // served from cache

        service.BrightenContent = false;
        byte[] plain = service.RenderSheetPng(composition, sheet, 200);

        Assert.NotEqual(bright, plain);
    }
}
