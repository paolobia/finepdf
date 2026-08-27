using FinePdf.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class PdfSharpDocumentService : IPdfDocumentService
{
    /// <inheritdoc />
    public SourceDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new PdfLoadException(path, $"File not found: {path}");
        }

        using PdfDocument document = OpenForImport(path);

        var pages = new List<SourcePage>(document.PageCount);
        for (int i = 0; i < document.PageCount; i++)
        {
            PdfPage page = document.Pages[i];
            double width = page.Width.Point;
            double height = page.Height.Point;

            // Present sizes as a reader would see them: honour the page's intrinsic /Rotate.
            if (Math.Abs(page.Rotate % 180) == 90)
            {
                (width, height) = (height, width);
            }

            pages.Add(new SourcePage(i, width, height));
        }

        if (pages.Count == 0)
        {
            throw new PdfLoadException(path, "The PDF has no pages.");
        }

        return new SourceDocument(path, pages);
    }

    private static PdfDocument OpenForImport(string path)
    {
        try
        {
            return PdfReader.Open(path, PdfDocumentOpenMode.Import);
        }
        catch (PdfReaderException ex)
        {
            bool isProtected =
                ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase);

            throw new PdfLoadException(
                path,
                isProtected ? "The PDF is password-protected." : $"The PDF could not be read: {ex.Message}",
                isProtected,
                ex);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or FormatException or ArgumentException)
        {
            throw new PdfLoadException(path, $"The PDF could not be read: {ex.Message}", isPasswordProtected: false, ex);
        }
    }
}
