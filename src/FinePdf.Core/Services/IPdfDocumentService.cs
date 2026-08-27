using FinePdf.Core.Models;

namespace FinePdf.Core.Services;

/// <summary>Loads a PDF file from disk into an immutable <see cref="SourceDocument"/> snapshot.</summary>
public interface IPdfDocumentService
{
    /// <summary>
    /// Reads page metadata from the PDF at <paramref name="path"/>.
    /// </summary>
    /// <exception cref="PdfLoadException">
    /// The file is missing, is not a readable PDF, or is password-protected.
    /// </exception>
    SourceDocument Load(string path);
}
