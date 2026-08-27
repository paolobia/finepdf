namespace FinePdf.Core;

/// <summary>
/// Raised by <see cref="Services.IPdfDocumentService"/> when a PDF cannot be opened —
/// missing, not a PDF, corrupt, or password-protected (<see cref="IsPasswordProtected"/>).
/// </summary>
public sealed class PdfLoadException : Exception
{
    public PdfLoadException(string path, string message, bool isPasswordProtected = false, Exception? innerException = null)
        : base(message, innerException)
    {
        Path = path;
        IsPasswordProtected = isPasswordProtected;
    }

    public PdfLoadException()
    {
        Path = string.Empty;
    }

    public PdfLoadException(string message)
        : base(message)
    {
        Path = string.Empty;
    }

    public PdfLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
        Path = string.Empty;
    }

    public string Path { get; }

    public bool IsPasswordProtected { get; }
}
