using System.Security.Cryptography;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class DocumentImportService : IDocumentImportService
{
    private const double ImageDpi = 96.0;
    private const double PointsPerInch = 72.0;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp",
    };

    private readonly string _cacheDirectory =
        Path.Combine(Path.GetTempPath(), $"finepdf-imports-{Environment.ProcessId}");

    private readonly object _gate = new();

    public bool CanImport(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string ext = Path.GetExtension(path);
        return ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) || ImageExtensions.Contains(ext);
    }

    public string EnsurePdf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (!ImageExtensions.Contains(Path.GetExtension(path)))
        {
            throw new DocumentImportException($"Formato non supportato: {Path.GetFileName(path)}");
        }

        lock (_gate)
        {
            Directory.CreateDirectory(_cacheDirectory);
            string outputPath = Path.Combine(_cacheDirectory, CacheKey(path) + ".pdf");
            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            ConvertImage(path, outputPath);
            return outputPath;
        }
    }

    public string ImportAsCopy(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string ext = Path.GetExtension(path);
        if (ImageExtensions.Contains(ext))
        {
            return EnsurePdf(path); // already produced inside the cache
        }

        if (!ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentImportException($"Formato non supportato: {Path.GetFileName(path)}");
        }

        lock (_gate)
        {
            Directory.CreateDirectory(_cacheDirectory);
            string outputPath = Path.Combine(_cacheDirectory, CacheKey(path) + ".pdf");
            if (!File.Exists(outputPath))
            {
                try
                {
                    File.Copy(path, outputPath, overwrite: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new DocumentImportException($"Impossibile leggere: {Path.GetFileName(path)}", ex);
                }
            }

            return outputPath;
        }
    }

    public void CleanupSession()
    {
        lock (_gate)
        {
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    Directory.Delete(_cacheDirectory, recursive: true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // best-effort
            }
        }
    }

    private static void ConvertImage(string imagePath, string outputPath)
    {
        byte[] pngBytes;
        try
        {
            using SKBitmap? bitmap = SKBitmap.Decode(imagePath)
                ?? throw new DocumentImportException($"Immagine non leggibile: {Path.GetFileName(imagePath)}");
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            pngBytes = data.ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DocumentImportException($"Immagine non leggibile: {Path.GetFileName(imagePath)}", ex);
        }

        using var pngStream = new MemoryStream(pngBytes);
        using XImage xImage = XImage.FromStream(pngStream);

        double widthPoints = xImage.PixelWidth / ImageDpi * PointsPerInch;
        double heightPoints = xImage.PixelHeight / ImageDpi * PointsPerInch;

        using var document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = XUnit.FromPoint(widthPoints);
        page.Height = XUnit.FromPoint(heightPoints);

        using (XGraphics gfx = XGraphics.FromPdfPage(page))
        {
            gfx.DrawImage(xImage, 0, 0, widthPoints, heightPoints);
        }

        document.Save(outputPath);
    }

    private static string CacheKey(string path)
    {
        long ticks = 0;
        try
        {
            ticks = File.GetLastWriteTimeUtc(path).Ticks;
        }
        catch (IOException)
        {
            // fall back to path-only key
        }

        byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes($"{Path.GetFullPath(path)}|{ticks}"));
        return Convert.ToHexString(hash);
    }
}
