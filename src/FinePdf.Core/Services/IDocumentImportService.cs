namespace FinePdf.Core.Services;

/// <summary>
/// Turns a source file the user picked in the "Cartella di lavoro" tab into a PDF path that
/// <see cref="IPdfDocumentService"/> can load. PDFs pass through unchanged; images
/// (<c>png/jpg/jpeg/bmp/gif/webp</c>) are converted to a one-page PDF in a per-session temp
/// cache. Other types are rejected by <see cref="CanImport"/>.
/// </summary>
public interface IDocumentImportService
{
    bool CanImport(string path);

    /// <returns>
    /// <paramref name="path"/> itself when it is already a PDF; otherwise the path of a
    /// generated temporary PDF.
    /// </returns>
    /// <exception cref="FinePdf.DocumentImportException">The file could not be converted.</exception>
    string EnsurePdf(string path);

    /// <summary>
    /// Like <see cref="EnsurePdf"/> but ALWAYS returns a path inside the session cache — a PDF
    /// is copied in rather than referenced in place. Use when the original may be moved/deleted
    /// (e.g. trashed) after import.
    /// </summary>
    /// <exception cref="FinePdf.DocumentImportException">The file could not be converted.</exception>
    string ImportAsCopy(string path);

    /// <summary>Deletes the per-session conversion cache. Call on shutdown.</summary>
    void CleanupSession();
}
