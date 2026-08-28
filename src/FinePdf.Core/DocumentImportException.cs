namespace FinePdf.Core;

/// <summary>
/// Raised by <see cref="Services.IDocumentImportService"/> when a non-PDF source file cannot be
/// converted to PDF (unreadable image, unsupported format). The UI turns it into a status message.
/// </summary>
public sealed class DocumentImportException : Exception
{
    public DocumentImportException()
    {
    }

    public DocumentImportException(string message)
        : base(message)
    {
    }

    public DocumentImportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
