using FinePdf.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FinePdf.Core.Tests.TestSupport;

/// <summary>
/// Builds tiny PDF files on disk at test time. Draws only rectangles, so no font resolver
/// is needed on any platform.
/// </summary>
internal static class PdfTestFactory
{
    public static string CreatePdf(
        string directory,
        string fileName,
        int pageCount,
        PaperSize? paper = null,
        PageOrientation orientation = PageOrientation.Portrait)
    {
        paper ??= PaperSize.A4;
        double width = orientation == PageOrientation.Portrait ? paper.WidthPoints : paper.HeightPoints;
        double height = orientation == PageOrientation.Portrait ? paper.HeightPoints : paper.WidthPoints;

        var pages = new (double Width, double Height)[pageCount];
        Array.Fill(pages, (width, height));
        return CreatePdfWithPageSizes(directory, fileName, pages);
    }

    public static string CreatePdfWithPageSizes(
        string directory,
        string fileName,
        params (double Width, double Height)[] pages)
    {
        string path = Path.Combine(directory, fileName);

        using var document = new PdfDocument();
        foreach ((double pageWidth, double pageHeight) in pages)
        {
            PdfPage page = document.AddPage();
            page.Width = XUnit.FromPoint(pageWidth);
            page.Height = XUnit.FromPoint(pageHeight);

            using XGraphics gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(XPens.Black, 3, 3, pageWidth - 6, pageHeight - 6);
            gfx.DrawRectangle(XBrushes.Black, 12, 12, 36, 36);
        }

        document.Save(path);
        return path;
    }
}
