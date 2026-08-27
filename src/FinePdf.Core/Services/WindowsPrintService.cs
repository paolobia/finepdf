#if WINDOWS
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using FinePdf.Core.Models;
using PDFtoImage;
using SkiaSharp;

namespace FinePdf.Core.Services;

/// <summary>
/// GDI printing for Windows. The composition is written to a temporary PDF via
/// <see cref="IPdfComposer"/>, then each selected sheet is rasterised with PDFium (PDFtoImage)
/// and drawn onto the <see cref="PrintDocument"/> page, so the printout matches the preview.
/// Only compiled in the <c>net8.0-windows</c> target.
/// </summary>
public sealed class WindowsPrintService : IPrintService
{
    private const int RasterDpi = 200;

    private readonly IPdfComposer _composer;

    public WindowsPrintService(IPdfComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        _composer = composer;
    }

    /// <inheritdoc />
    public IReadOnlyList<PrinterInfo> GetPrinters()
    {
        try
        {
            string defaultName = new PrinterSettings().PrinterName;
            var printers = new List<PrinterInfo>();
            foreach (string? name in PrinterSettings.InstalledPrinters)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    printers.Add(new PrinterInfo(name, string.Equals(name, defaultName, StringComparison.Ordinal)));
                }
            }

            return printers;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidPrinterException)
        {
            throw new PrintException("Impossibile elencare le stampanti.", ex);
        }
    }

    /// <inheritdoc />
    public void Print(Composition composition, PrintOptions options)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(options);
        options.Validated();

        IReadOnlyList<int> sheets = options.Range.Resolve(composition.Sheets.Count);
        if (sheets.Count == 0)
        {
            throw new PrintException("L'intervallo indicato non seleziona alcun foglio.");
        }

        byte[] pdfBytes = ComposeToBytes(composition);
        int rasterWidth = (int)Math.Round(composition.SheetSizePoints.Width / 72.0 * RasterDpi);

        using var printDocument = new PrintDocument { DocumentName = "FinePdf" };

        try
        {
            printDocument.PrinterSettings.PrinterName = options.PrinterName;
        }
        catch (ArgumentException ex)
        {
            throw new PrintException($"Stampante non valida: {options.PrinterName}.", ex);
        }

        if (!printDocument.PrinterSettings.IsValid)
        {
            throw new PrintException($"Stampante non disponibile: {options.PrinterName}.");
        }

        printDocument.PrinterSettings.Copies = (short)Math.Clamp(options.Copies, 1, (int)short.MaxValue);
        ApplyDuplex(printDocument.PrinterSettings, options.Duplex);

        int cursor = 0;
        printDocument.PrintPage += (_, e) =>
        {
            DrawSheet(e, pdfBytes, sheets[cursor] - 1, rasterWidth);
            cursor++;
            e.HasMorePages = cursor < sheets.Count;
        };

        try
        {
            printDocument.Print();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidPrinterException or InvalidOperationException)
        {
            throw new PrintException("Invio alla stampante non riuscito.", ex);
        }
    }

    private byte[] ComposeToBytes(Composition composition)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"finepdf-{Guid.NewGuid():N}.pdf");
        try
        {
            _composer.Compose(composition, tempPath);
            return File.ReadAllBytes(tempPath);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (IOException)
            {
                // Temp cleanup is best-effort.
            }
        }
    }

    private static void ApplyDuplex(PrinterSettings settings, DuplexMode duplex)
    {
        if (duplex == DuplexMode.Simplex || !settings.CanDuplex)
        {
            settings.Duplex = Duplex.Simplex;
            return;
        }

        settings.Duplex = duplex == DuplexMode.ShortEdge ? Duplex.Horizontal : Duplex.Vertical;
    }

    private static void DrawSheet(PrintPageEventArgs e, byte[] pdfBytes, int pageIndex, int rasterWidth)
    {
        var renderOptions = new RenderOptions { Width = rasterWidth, WithAspectRatio = true };
        using SKBitmap bitmap = Conversion.ToImage(pdfBytes, password: null, page: (Index)pageIndex, options: renderOptions);

        using var buffer = new MemoryStream();
        bitmap.Encode(buffer, SKEncodedImageFormat.Png, 100);
        buffer.Position = 0;
        using var image = Image.FromStream(buffer);

        Graphics graphics = e.Graphics ?? throw new PrintException("Contesto grafico di stampa non disponibile.");
        RectangleF area = e.MarginBounds;
        float scale = Math.Min(area.Width / image.Width, area.Height / image.Height);
        float width = image.Width * scale;
        float height = image.Height * scale;
        float x = area.Left + ((area.Width - width) / 2f);
        float y = area.Top + ((area.Height - height) / 2f);
        graphics.DrawImage(image, x, y, width, height);
    }
}
#endif
