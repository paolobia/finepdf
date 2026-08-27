namespace FinePdf.Core;

/// <summary>
/// Raised by <see cref="Services.IPrintService"/> when enumerating printers or sending a job
/// fails. The UI turns it into a dialog.
/// </summary>
public sealed class PrintException : Exception
{
    public PrintException()
    {
    }

    public PrintException(string message)
        : base(message)
    {
    }

    public PrintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
